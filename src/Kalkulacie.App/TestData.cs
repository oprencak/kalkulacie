#if DEBUG
using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;

namespace Kalkulacie.App;

/// <summary>
/// Testovacie dáta pre vývoj. Spustenie s <see cref="Argument"/> (profil „Test“ v launchSettings.json)
/// otvorí <c>data\Test.db</c> a ak je prázdna, naplní ju vzorovými dátami. Len v Debug zostavení —
/// do publikovaného exe sa nedostane. Čistý stav = zmazať Test.db.
/// </summary>
internal static class TestData
{
    public const string Argument = "--test-data";

    public static string DbPath => Path.Combine(AppDb.DataDir, "Test.db");

    /// <summary>Naplní otvorenú databázu, ak v nej ešte nie sú žiadne materiály, réžie ani výrobky.</summary>
    public static void SeedIfEmpty()
    {
        List<int> productIds;
        using (var db = AppDb.CreateContext())
        {
            if (db.Materials.Any() || db.Operations.Any() || db.Products.Any()) return;

            // Plošné materiály (cena za m²)
            var lepenka3 = new Material { Name = "Lepenka 3-vrstvová", Unit = "m2", Price = 0.42m };
            var lepenka5 = new Material { Name = "Lepenka 5-vrstvová", Unit = "m2", Price = 0.78m };
            var kartón = new Material { Name = "Kartón hladký 300 g", Unit = "m2", Price = 0.55m };
            var fólia = new Material { Name = "Fólia PE číra", Unit = "m2", Price = 0.19m };
            var pena = new Material { Name = "Penová výstelka 10 mm", Unit = "m2", Price = 2.35m };
            // Kusové materiály
            var lepidlo = new Material { Name = "Lepidlo disperzné", Unit = "kg", Price = 3.10m };
            var páska = new Material { Name = "Lepiaca páska 48 mm", Unit = "m", Price = 0.012m };
            var etiketa = new Material { Name = "Etiketa samolepiaca", Unit = "ks", Price = 0.035m };
            var spona = new Material { Name = "Spona oceľová", Unit = "ks", Price = 0.004m };
            var rúčka = new Material { Name = "Rúčka plastová", Unit = "ks", Price = 0.11m };
            var nepoužitý = new Material { Name = "Papier baliaci (nepoužitý)", Unit = "m2", Price = 0.08m };

            var rezanie = new Operation { Name = "Rezanie", Rate = 14.50m };
            var ohýbanie = new Operation { Name = "Ohýbanie a ryhovanie", Rate = 12.00m };
            var lepenie = new Operation { Name = "Lepenie ručné", Rate = 9.80m };
            var šitie = new Operation { Name = "Zošívanie sponami", Rate = 11.20m };
            var potlač = new Operation { Name = "Potlač 1 farba", Rate = 18.00m };
            var balenie = new Operation { Name = "Balenie a paletizácia", Rate = 8.50m };
            var stroj = new Operation { Name = "Strojová linka — hodina", Rate = 46.00m };
            var nepoužitá = new Operation { Name = "Kontrola kvality (nepoužitá)", Rate = 10.00m };

            db.Materials.AddRange(lepenka3, lepenka5, kartón, fólia, pena, lepidlo, páska, etiketa, spona, rúčka, nepoužitý);
            db.Operations.AddRange(rezanie, ohýbanie, lepenie, šitie, potlač, balenie, stroj, nepoužitá);

            Product[] products =
            [
                Make("Krabica klopová 300×200×150", CalculationMode.Manual,
                    Area(lepenka3, 1.05m, 0.38m), Piece(lepidlo, 0.012m), Piece(páska, 1.2m), Piece(etiketa, 1),
                    Work(rezanie, 120), Work(ohýbanie, 90), Work(lepenie, 60), Work(balenie, 200)),
                Make("Krabica klopová 300×200×150", CalculationMode.Automatic,
                    Area(lepenka3, 1.05m, 0.38m), Piece(lepidlo, 0.010m), Piece(etiketa, 1),
                    Work(stroj, 900), Work(balenie, 400)),
                Make("Krabica klopová 600×400×400", CalculationMode.Manual,
                    Area(lepenka5, 2.06m, 0.82m), Piece(spona, 12), Piece(páska, 2.4m),
                    Work(rezanie, 80), Work(ohýbanie, 60), Work(šitie, 45), Work(balenie, 120)),
                Make("Krabica klopová 600×400×400", CalculationMode.Automatic,
                    Area(lepenka5, 2.06m, 0.82m), Piece(lepidlo, 0.025m),
                    Work(stroj, 520), Work(balenie, 240)),
                Make("Krabica darčeková s rúčkou", CalculationMode.Manual,
                    Area(kartón, 0.72m, 0.46m), Area(fólia, 0.20m, 0.15m), Piece(rúčka, 1), Piece(lepidlo, 0.008m),
                    Work(rezanie, 150), Work(potlač, 250), Work(ohýbanie, 70), Work(lepenie, 40)),
                Make("Preklad kartónový 580×380", CalculationMode.Automatic,
                    Area(lepenka3, 0.58m, 0.38m), Work(stroj, 2400)),
                Make("Obal s penovou výstelkou", CalculationMode.Manual,
                    Area(lepenka5, 1.40m, 0.60m), Area(pena, 0.40m, 0.30m, 2), Piece(lepidlo, 0.030m), Piece(etiketa, 2),
                    Work(rezanie, 60), Work(lepenie, 25), Work(balenie, 80)),
                Make("Tubus poštový 700 mm", CalculationMode.Manual,
                    Area(kartón, 0.70m, 0.28m), Piece(páska, 0.6m), Work(rezanie, 200), Work(lepenie, 90)),
                Make("Fixačná vložka L", CalculationMode.Automatic,
                    Area(lepenka3, 0.32m, 0.24m), Work(stroj, 3600)),
                // Bez zloženia — na skúšanie upozornení a nulových cien
                Make("Nový výrobok bez zloženia", CalculationMode.Manual),
            ];
            db.Products.AddRange(products);
            db.SaveChanges();
            productIds = products.Select(p => p.Id).ToList();
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        SaveEdition("Interná kalkulácia — všetky výrobky", today, "Testovacie dáta: všetky stĺpce.", "", productIds);
        SaveEdition("Cenník pre zákazníka", today.AddDays(-14), "Testovacie dáta: tlačí sa len výrobok a cena.",
            "type,material,labor,uvn,calculatedAt", productIds.Take(5));
    }

    private static void SaveEdition(string name, DateOnly date, string note, string hiddenColumns, IEnumerable<int> productIds) =>
        CalculationEditions.Save(new CalculationEdition
        {
            Name = name, Date = date, Note = note, HiddenColumns = hiddenColumns,
            Products = CalculationEditions.Calculate(productIds),
        });

    private static Product Make(string name, CalculationMode mode, params CompositionRow[] rows)
    {
        for (int i = 0; i < rows.Length; i++)
            rows[i].Position = i + 1;
        return new Product { Name = name, Mode = mode, CompositionRows = rows.ToList() };
    }

    private static CompositionRow Area(Material material, decimal a, decimal b, decimal quantity = 1) =>
        new() { RowType = RowType.Material, Material = material, DimensionA = a, DimensionB = b, Quantity = quantity };

    private static CompositionRow Piece(Material material, decimal quantity) =>
        new() { RowType = RowType.Material, Material = material, Quantity = quantity };

    /// <param name="pieces">Počet kusov, ktorým sa delí sadzba.</param>
    private static CompositionRow Work(Operation operation, decimal pieces) =>
        new() { RowType = RowType.Operation, Operation = operation, Quantity = pieces };
}
#endif
