using System.Buffers.Binary;
using System.Text;

namespace Kalkulacie.Import.Dbf;

public sealed class DbfReader
{
    private readonly Encoding _encoding;

    public DbfReader(Encoding? encoding = null)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _encoding = encoding ?? Encoding.GetEncoding(1250);
    }

    public IReadOnlyList<DbfRecord> ReadRecords(string path)
    {
        using var stream = File.OpenRead(path);
        var table = ReadTable(path, stream, out var languageDriverId);
        var decode = ResolveDecoder(languageDriverId);

        if (stream.Position < table.HeaderLength)
            stream.Seek(table.HeaderLength, SeekOrigin.Begin);

        var records = new List<DbfRecord>();
        var buffer = new byte[table.RecordLength];

        for (var i = 0; i < table.RecordCount; i++)
        {
            ReadExactly(stream, buffer);

            if (buffer[0] == (byte)'*') continue; // preskočiť zmazané

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in table.Fields)
            {
                values[field.Name] = DecodeValue(buffer.AsSpan(field.Offset, field.Length), field.Type, decode);
            }
            records.Add(new DbfRecord(false, values));
        }

        return records;
    }

    private DbfTable ReadTable(string path, Stream stream, out byte languageDriverId)
    {
        Span<byte> header = stackalloc byte[32];
        ReadExactly(stream, header);
        languageDriverId = header[29];

        var version = header[0];
        var recordCount = BinaryPrimitives.ReadInt32LittleEndian(header[4..8]);
        var headerLength = BinaryPrimitives.ReadUInt16LittleEndian(header[8..10]);
        var recordLength = BinaryPrimitives.ReadUInt16LittleEndian(header[10..12]);
        var year = 1900 + header[1];
        var month = Math.Max(1, (int)header[2]);
        var day = Math.Max(1, (int)header[3]);

        var fields = new List<DbfField>();
        while (true)
        {
            var descriptor = new byte[32];
            var first = stream.ReadByte();
            if (first < 0) throw new EndOfStreamException();
            if (first == 0x0D) break;
            descriptor[0] = (byte)first;
            ReadExactly(stream, descriptor.AsSpan(1));

            var name = ReadNullTerminated(descriptor.AsSpan(0, 11));
            var type = (char)descriptor[11];
            var offset = BinaryPrimitives.ReadInt32LittleEndian(descriptor.AsSpan(12, 4));
            var length = descriptor[16];
            var dec = descriptor[17];
            fields.Add(new DbfField(name, type, length, dec, offset));
        }

        return new DbfTable(path, version, recordCount, headerLength, recordLength,
            new DateOnly(year, month, day), fields);
    }

    private delegate string Decoder(ReadOnlySpan<byte> bytes);

    private static string DecodeValue(ReadOnlySpan<byte> value, char type, Decoder decode)
    {
        var trimmed = Trim(value);
        if (trimmed.IsEmpty) return string.Empty;
        return type is 'N' or 'F' or 'B' or 'Y' or 'D' or 'L'
            ? Encoding.ASCII.GetString(trimmed)
            : decode(trimmed);
    }

    /// <summary>
    /// Kódovanie podľa Language Driver ID v hlavičke DBF (bajt 29).
    /// Pôvodná aplikácia má tabuľky v rôznych kódovaniach (napr. VYROBKY.DBF v Kamenickom,
    /// ostatné vo Windows-1250), preto sa kódovanie určuje pre každý súbor zvlášť.
    /// </summary>
    private Decoder ResolveDecoder(byte languageDriverId) => languageDriverId switch
    {
        0x68 => DecodeKamenicky,                                    // Kamenický (MS-DOS 895)
        0x64 => bytes => Encoding.GetEncoding(852).GetString(bytes),  // Eastern European MS-DOS
        0xC8 => bytes => Encoding.GetEncoding(1250).GetString(bytes), // Eastern European Windows
        0x01 => bytes => Encoding.GetEncoding(437).GetString(bytes),  // US MS-DOS
        _ => bytes => _encoding.GetString(bytes),
    };

    // Kamenický (CP895): 0x80–0xAF sú česko-slovenské znaky, zvyšok je zhodný s CP437.
    private const string KamenickyHigh =
        "ČüéďäĎŤčěĚĹÍľĺÄÁ" +   // 0x80–0x8F
        "ÉžŽôöÓůÚýÖÜŠĽÝŘť" +   // 0x90–0x9F
        "áíóúňŇŮÔšřŕŔ¼§«»";    // 0xA0–0xAF

    private static string DecodeKamenicky(ReadOnlySpan<byte> bytes)
    {
        var cp437 = Encoding.GetEncoding(437);
        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
        {
            if (b < 0x80) sb.Append((char)b);
            else if (b < 0xB0) sb.Append(KamenickyHigh[b - 0x80]);
            else sb.Append(cp437.GetString([b]));
        }
        return sb.ToString();
    }

    private string ReadNullTerminated(ReadOnlySpan<byte> bytes)
    {
        var len = bytes.IndexOf((byte)0);
        return Encoding.ASCII.GetString(len >= 0 ? bytes[..len] : bytes).Trim();
    }

    private static ReadOnlySpan<byte> Trim(ReadOnlySpan<byte> value)
    {
        var s = 0; var e = value.Length - 1;
        while (s <= e && value[s] == ' ') s++;
        while (e >= s && value[e] == ' ') e--;
        return s > e ? ReadOnlySpan<byte>.Empty : value[s..(e + 1)];
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer[total..]);
            if (read == 0) throw new EndOfStreamException();
            total += read;
        }
    }
}
