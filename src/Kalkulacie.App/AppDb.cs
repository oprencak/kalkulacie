using Kalkulacie.Core;
using Microsoft.EntityFrameworkCore;

namespace Kalkulacie.App;

public static class AppDb
{
    private static string DbPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Kalkulacie",
        "kalkulacie.db");

    public static KalkulacieDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<KalkulacieDbContext>()
            .UseSqlite($"Data Source={DbPath}")
            .Options;
        return new KalkulacieDbContext(options);
    }

    public static void EnsureDatabase()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
        using var db = CreateContext();
        db.Database.Migrate();
    }
}
