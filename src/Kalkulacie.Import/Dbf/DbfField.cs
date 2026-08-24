namespace Kalkulacie.Import.Dbf;

public sealed record DbfField(
    string Name,
    char Type,
    int Length,
    int DecimalCount,
    int Offset);
