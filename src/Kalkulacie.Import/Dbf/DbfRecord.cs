namespace Kalkulacie.Import.Dbf;

public sealed class DbfRecord
{
    public DbfRecord(bool isDeleted, IReadOnlyDictionary<string, string> values)
    {
        IsDeleted = isDeleted;
        Values = values;
    }

    public bool IsDeleted { get; }
    public IReadOnlyDictionary<string, string> Values { get; }

    public string? GetValue(string fieldName) =>
        Values.TryGetValue(fieldName, out var value) ? value : null;
}
