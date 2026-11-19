using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;

namespace Kalkulacie.Core.Calculations;

public static class PriceWarnings
{
    public static IEnumerable<string> ForProduct(Product product)
    {
        if (product.CompositionRows.Count == 0)
            yield return "výrobok bez zloženia";

        foreach (var row in product.CompositionRows.Where(r => r.RowType == RowType.Material).OrderBy(r => r.Position))
        {
            var label = $"riadok {row.Position} ({row.Material?.Name ?? "materiál"})";
            if (row.Material?.Price == 0)
                yield return $"{label} · nulová cena materiálu";

            // Kusový riadok má oba rozmery prázdne. Vyplnený rozmer označuje plošný riadok.
            if ((row.DimensionA.HasValue || row.DimensionB.HasValue) &&
                (row.DimensionA.GetValueOrDefault() <= 0 || row.DimensionB.GetValueOrDefault() <= 0))
            {
                yield return $"{label} · chýbajúci alebo neplatný rozmer plošného materiálu";
            }
        }
    }
}
