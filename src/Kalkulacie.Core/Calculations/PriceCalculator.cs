using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;

namespace Kalkulacie.Core.Calculations;

/// <summary>
/// Cenové vzorce z FoxPro KALK.PRG. Koeficienty a zaokrúhľovanie prichádzajú z <see cref="PricingSettings"/>;
/// zaokrúhľuje sa každý riadok zloženia aj každá výsledná cena (ako v origináli).
/// </summary>
public static class PriceCalculator
{
    public static decimal MaterialCost(CompositionRow row, decimal unitPrice, PricingSettings settings)
    {
        if (row.DimensionA.HasValue && row.DimensionB.HasValue
            && row.DimensionA.Value != 0 && row.DimensionB.Value != 0)
        {
            // Plošný materiál (fólia/plech)
            decimal coefficient = row.Product.Mode == CalculationMode.Manual
                ? settings.AreaMaterialManual
                : settings.AreaMaterialAutomatic;
            return settings.Round(row.DimensionA.Value * row.DimensionB.Value * coefficient * unitPrice * row.Quantity);
        }

        // Kusový materiál
        return settings.Round(unitPrice * row.Quantity * settings.PieceMaterial);
    }

    public static decimal OperationCost(decimal rate, decimal quantity, PricingSettings settings)
    {
        if (quantity == 0) return 0;
        return settings.Round(rate / quantity);
    }

    /// <summary>Náklad jedného riadku zloženia (materiál aj réžia); 0, ak riadok nemá položku.</summary>
    public static decimal RowCost(CompositionRow row, PricingSettings settings) => row.RowType switch
    {
        RowType.Material when row.Material != null => MaterialCost(row, row.Material.Price, settings),
        RowType.Operation when row.Operation != null => OperationCost(row.Operation.Rate, row.Quantity, settings),
        _ => 0
    };

    public static PriceSummary Calculate(Product product, PricingSettings settings)
    {
        decimal materialTotal = 0;
        decimal operationTotal = 0;

        foreach (var row in product.CompositionRows)
        {
            if (row.RowType == RowType.Material)
                materialTotal += RowCost(row, settings);
            else if (row.RowType == RowType.Operation)
                operationTotal += RowCost(row, settings);
        }

        decimal uvn = settings.Round(materialTotal + operationTotal);
        decimal margin = product.Mode == CalculationMode.Manual ? settings.MarginManual : settings.MarginAutomatic;
        decimal basePrice = settings.Round(uvn * margin);

        return new PriceSummary(
            MaterialTotal: materialTotal,
            OperationTotal: operationTotal,
            Uvn: uvn,
            BasePrice: basePrice
        );
    }
}

public record PriceSummary(
    decimal MaterialTotal,
    decimal OperationTotal,
    decimal Uvn,
    decimal BasePrice
);
