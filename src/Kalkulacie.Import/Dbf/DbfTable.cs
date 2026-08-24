namespace Kalkulacie.Import.Dbf;

public sealed class DbfTable
{
    public DbfTable(
        string path, byte version, int recordCount,
        int headerLength, int recordLength,
        DateOnly lastUpdated, IReadOnlyList<DbfField> fields)
    {
        Path = path;
        Version = version;
        RecordCount = recordCount;
        HeaderLength = headerLength;
        RecordLength = recordLength;
        LastUpdated = lastUpdated;
        Fields = fields;
    }

    public string Path { get; }
    public byte Version { get; }
    public int RecordCount { get; }
    public int HeaderLength { get; }
    public int RecordLength { get; }
    public DateOnly LastUpdated { get; }
    public IReadOnlyList<DbfField> Fields { get; }
}
