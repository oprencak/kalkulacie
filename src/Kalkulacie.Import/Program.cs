using System.Globalization;
using System.Text;
using Kalkulacie.Core;
using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;
using Kalkulacie.Import.Dbf;
using Microsoft.EntityFrameworkCore;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var sourceDir = @"C:\dev\duri\Cennik\DATA";
var dbPath = Path.Combine(AppContext.BaseDirectory, "data", "kalkulacie.db");

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--source" or "-s" when i + 1 < args.Length:
            sourceDir = args[++i];
            break;
        case "--db" when i + 1 < args.Length:
            dbPath = args[++i];
            break;
        case "--help" or "-h":
            Console.WriteLine("Použitie: Kalkulacie.Import [--source <cesta>] [--db <cesta>]");
            Console.WriteLine($"  --source  Priečinok s DBF súbormi (default: {sourceDir})");
            Console.WriteLine($"  --db      Cesta k SQLite databáze   (default: data\\kalkulacie.db vedľa .exe)");
            return 0;
    }
}

if (!Directory.Exists(sourceDir))
{
    Console.Error.WriteLine($"Zdrojový priečinok neexistuje: {sourceDir}");
    return 1;
}

Console.WriteLine($"Zdroj:     {sourceDir}");
Console.WriteLine($"Databáza:  {dbPath}");
Console.WriteLine();

Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

// Vymazanie existujúceho súboru — import vždy vytvára DB od nuly
if (File.Exists(dbPath)) File.Delete(dbPath);

var options = new DbContextOptionsBuilder<KalkulacieDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

using var db = new KalkulacieDbContext(options);
db.Database.Migrate();

var reader = new DbfReader(Encoding.GetEncoding("windows-1250"));

// --- Materiály ---
Console.Write("Materiály (VSTUPY.DBF)...     ");
var materials = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
foreach (var r in ReadDbf(reader, sourceDir, "VSTUPY.DBF"))
{
    var name = r.GetValue("NAZOV_MAT") ?? "";
    if (string.IsNullOrWhiteSpace(name) || materials.ContainsKey(name)) continue;
    var mat = new Material { Name = name, Unit = NullIfEmpty(r.GetValue("MJ")), Price = ParseDecimal(r.GetValue("CENA_MAT")) };
    db.Materials.Add(mat);
    materials[name] = mat;
}
db.SaveChanges();
Console.WriteLine($"{materials.Count} záznamov");

// --- Réžia/Práca ---
Console.Write("Réžia/Práca (REZIA.DBF)...    ");
var operations = new Dictionary<string, Operation>(StringComparer.OrdinalIgnoreCase);
foreach (var r in ReadDbf(reader, sourceDir, "REZIA.DBF"))
{
    var name = r.GetValue("NAZOV_PR") ?? "";
    if (string.IsNullOrWhiteSpace(name) || operations.ContainsKey(name)) continue;
    var op = new Operation { Name = name, Rate = ParseDecimal(r.GetValue("PRACA")) };
    db.Operations.Add(op);
    operations[name] = op;
}
db.SaveChanges();
Console.WriteLine($"{operations.Count} záznamov");

// --- Výrobky ---
Console.Write("Výrobky (VYROBKY + A_VYR)...  ");
var products = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
ImportProducts("VYROBKY.DBF", CalculationMode.Manual);
ImportProducts("A_VYR.DBF", CalculationMode.Automatic);

// Výrobky referencované v ZLOZENIE ale chýbajúce vo VYROBKY
ImportProductsFromComposition("ZLOZENIE.DBF", CalculationMode.Manual);
ImportProductsFromComposition("A_ZLOZ.DBF", CalculationMode.Automatic);

db.SaveChanges();
Console.WriteLine($"{products.Count} záznamov");

// --- Zloženie ---
Console.Write("Zloženie (ZLOZENIE + A_ZLOZ). ");
var compCount = 0;
var skipped = 0;
compCount += ImportComposition("ZLOZENIE.DBF", CalculationMode.Manual);
compCount += ImportComposition("A_ZLOZ.DBF", CalculationMode.Automatic);
db.SaveChanges();
Console.WriteLine($"{compCount} riadkov" + (skipped > 0 ? $" ({skipped} preskočených — neznámy výrobok/materiál)" : ""));

Console.WriteLine();
Console.WriteLine("Import dokončený.");
return 0;

// -----------------------------------------------------------------------

void ImportProductsFromComposition(string fileName, CalculationMode mode)
{
    foreach (var r in ReadDbf(reader, sourceDir, fileName))
    {
        var name = r.GetValue("NAZOV_VYR") ?? "";
        if (string.IsNullOrWhiteSpace(name)) continue;
        var key = $"{mode}:{name}";
        if (products.ContainsKey(key)) continue;
        var prod = new Product { Name = name, Mode = mode };
        db.Products.Add(prod);
        products[key] = prod;
    }
}

void ImportProducts(string fileName, CalculationMode mode)
{
    foreach (var r in ReadDbf(reader, sourceDir, fileName))
    {
        var name = r.GetValue("NAZOV_VYR") ?? "";
        if (string.IsNullOrWhiteSpace(name)) continue;
        var key = $"{mode}:{name}";
        if (products.ContainsKey(key)) continue;
        var prod = new Product { Name = name, Mode = mode };
        db.Products.Add(prod);
        products[key] = prod;
    }
}

int ImportComposition(string fileName, CalculationMode mode)
{
    var count = 0;
    // Poradie riadkov v zložení = poradie záznamov v DBF
    var positions = new Dictionary<Product, int>();
    foreach (var r in ReadDbf(reader, sourceDir, fileName))
    {
        var productName = r.GetValue("NAZOV_VYR") ?? "";
        if (string.IsNullOrWhiteSpace(productName)) continue;

        var key = $"{mode}:{productName}";
        if (!products.TryGetValue(key, out var product)) { skipped++; continue; }

        var matName = r.GetValue("NAZOV_MAT") ?? "";
        var opName  = r.GetValue("NAZOV_PR") ?? "";
        var qty     = ParseDecimal(r.GetValue("POCET_KS"));
        var dimA    = ParseDecimal(r.GetValue("ROZMER_A"));
        var dimB    = ParseDecimal(r.GetValue("ROZMER_B"));

        CompositionRow row;
        if (!string.IsNullOrWhiteSpace(matName) && materials.TryGetValue(matName, out var mat))
        {
            row = new CompositionRow
            {
                Product = product, RowType = RowType.Material, Material = mat,
                DimensionA = dimA == 0 ? null : dimA,
                DimensionB = dimB == 0 ? null : dimB,
                Quantity = qty
            };
        }
        else if (!string.IsNullOrWhiteSpace(opName) && operations.TryGetValue(opName, out var op))
        {
            row = new CompositionRow
            {
                Product = product, RowType = RowType.Operation, Operation = op,
                Quantity = qty
            };
        }
        else { skipped++; continue; }

        row.Position = positions[product] = positions.GetValueOrDefault(product) + 1;
        db.CompositionRows.Add(row);
        count++;
    }
    return count;
}

static IReadOnlyList<DbfRecord> ReadDbf(DbfReader r, string dir, string file)
{
    var path = Path.Combine(dir, file);
    return File.Exists(path) ? r.ReadRecords(path) : [];
}

static decimal ParseDecimal(string? value) =>
    decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : 0m;

static string? NullIfEmpty(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value;
