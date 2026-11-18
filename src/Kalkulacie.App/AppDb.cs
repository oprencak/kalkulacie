using Kalkulacie.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kalkulacie.App;

/// <summary>
/// Databázy ležia v priečinku <c>data</c> vedľa .exe — celý priečinok aplikácie je prenosný.
/// V priečinku môže byť viac *.db súborov; aktívnu vyberá používateľ.
/// </summary>
public static class AppDb
{
    public const string DefaultDbFileName = "kalkulacie.db";
    private const string LastDbFileName = "posledna-db.txt";

    public static string DataDir { get; } = Path.Combine(AppContext.BaseDirectory, "data");

    public static string? DbPath { get; private set; }

    private const int BackupsToKeep = 10;

    public static string BackupDir { get; } = Path.Combine(DataDir, "zalohy");

    /// <summary>Chyba poslednej automatickej zálohy, alebo null, ak prebehla v poriadku.</summary>
    public static string? BackupError { get; private set; }

    /// <summary>Názov aktívnej databázy (bez prípony) na zobrazenie.</summary>
    public static string DbName => DbPath is null ? "" : Path.GetFileNameWithoutExtension(DbPath);

    public static KalkulacieDbContext CreateContext() =>
        CreateContext(DbPath ?? throw new InvalidOperationException("Nie je vybraná databáza."));

    private static KalkulacieDbContext CreateContext(string path)
    {
        var options = new DbContextOptionsBuilder<KalkulacieDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;
        return new KalkulacieDbContext(options);
    }

    /// <summary>Overí názov novej DB; vráti chybovú hlášku alebo null, ak je názov v poriadku.</summary>
    public static string? ValidateNewName(string name)
    {
        name = name.Trim();
        if (name.Length == 0) return "Zadajte názov databázy.";
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return "Názov obsahuje nepovolené znaky ( \\ / : * ? \" < > | ).";
        if (File.Exists(Path.Combine(DataDir, name + ".db")))
            return $"Databáza „{name}“ už existuje.";
        return null;
    }

    /// <summary>Vytvorí novú prázdnu DB v priečinku data (neprepína na ňu). Vráti cestu.</summary>
    public static string Create(string name)
    {
        var error = ValidateNewName(name);
        if (error is not null) throw new InvalidOperationException(error);

        var path = Path.Combine(DataDir, name.Trim() + ".db");
        using (var db = CreateContext(path))
            db.Database.Migrate();
        SqliteConnection.ClearAllPools();
        return path;
    }

    /// <summary>Počty záznamov v DB — na zobrazenie pri potvrdení mazania.</summary>
    public static (int Products, int Materials, int Operations)? GetCounts(string path)
    {
        try
        {
            using var db = CreateContext(path);
            return (db.Products.Count(), db.Materials.Count(), db.Operations.Count());
        }
        catch
        {
            return null;
        }
        finally
        {
            SqliteConnection.ClearAllPools();
        }
    }

    /// <summary>Natrvalo zmaže DB súbor (aj -wal/-shm). Aktuálne otvorenú DB zmazať nejde.</summary>
    public static void Delete(string path)
    {
        if (string.Equals(Path.GetFullPath(path), DbPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Aktuálne otvorenú databázu nie je možné zmazať.");

        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "-wal", "-shm", "" })
        {
            if (File.Exists(path + suffix))
                File.Delete(path + suffix);
        }
    }

    /// <summary>
    /// Pripraví priečinok data: vytvorí ho a presunie doň starú DB uloženú priamo vedľa .exe.
    /// </summary>
    public static void PrepareDataDir()
    {
        Directory.CreateDirectory(DataDir);

        var legacy = Path.Combine(AppContext.BaseDirectory, DefaultDbFileName);
        var target = Path.Combine(DataDir, DefaultDbFileName);
        if (File.Exists(legacy) && !File.Exists(target))
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                if (File.Exists(legacy + suffix))
                    File.Move(legacy + suffix, target + suffix);
            }
        }
    }

    /// <summary>Všetky *.db súbory v priečinku data, zoradené podľa názvu.</summary>
    public static List<string> ListDatabases() =>
        Directory.EnumerateFiles(DataDir, "*.db")
            .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>Cesta k naposledy použitej DB, ak ešte existuje.</summary>
    public static string? LastUsed
    {
        get
        {
            try
            {
                var file = Path.Combine(DataDir, LastDbFileName);
                if (!File.Exists(file)) return null;
                var path = Path.Combine(DataDir, File.ReadAllText(file).Trim());
                return File.Exists(path) ? path : null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Zálohuje DB do data\zalohy pred jej otvorením (teda aj pred migráciou). Preskočí sa,
    /// ak sa DB od poslednej zálohy nezmenila; ponecháva sa posledných <see cref="BackupsToKeep"/> záloh.
    /// Zlyhanie zálohy neblokuje otvorenie DB — chyba ostane v <see cref="BackupError"/>.
    /// </summary>
    private static void Backup(string path)
    {
        BackupError = null;
        try
        {
            if (!File.Exists(path)) return;
            Directory.CreateDirectory(BackupDir);

            var name = Path.GetFileNameWithoutExtension(path);
            var existing = Directory.GetFiles(BackupDir, $"{name}_????????_??????.db")
                .Where(f => Path.GetFileName(f).Length == name.Length + "_yyyyMMdd_HHmmss.db".Length)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var changed = new[] { path, path + "-wal" }.Where(File.Exists).Max(File.GetLastWriteTimeUtc);
            if (existing.Count > 0 && File.GetLastWriteTimeUtc(existing[^1]) >= changed) return;

            var target = Path.Combine(BackupDir, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            using (var source = new SqliteConnection($"Data Source={path}"))
            using (var dest = new SqliteConnection($"Data Source={target}"))
            {
                source.Open();
                dest.Open();
                source.BackupDatabase(dest);
            }
            SqliteConnection.ClearAllPools();

            existing.Add(target);
            foreach (var old in existing.Take(Math.Max(0, existing.Count - BackupsToKeep)))
                File.Delete(old);
        }
        catch (Exception ex)
        {
            BackupError = ex.Message;
        }
    }

    /// <summary>Prepne aplikáciu na zadanú DB a aplikuje migrácie.</summary>
    public static void Open(string path)
    {
        // Uvoľní zámky na predchádzajúcej DB
        SqliteConnection.ClearAllPools();

        DbPath = Path.GetFullPath(path);
        Backup(DbPath);
        using (var db = CreateContext())
        {
            // Migrate() od EF Core 9 vždy zapíše do DB (zámok migrácií), takže by sa zmenil čas súboru
            // a pri každom štarte by vznikla nová záloha — volá sa preto len pri čakajúcich migráciách.
            if (db.Database.GetPendingMigrations().Any())
                db.Database.Migrate();
        }

        try
        {
            File.WriteAllText(Path.Combine(DataDir, LastDbFileName), Path.GetFileName(DbPath));
        }
        catch
        {
            // Zapamätanie výberu nie je kritické
        }
    }
}
