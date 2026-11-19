using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;

namespace Kalkulacie.Core.Calculations;

public sealed record SavedCalculation(
    CalculationMode Mode,
    DateTime CalculatedAtUtc,
    PriceSummary Summary,
    SavedPricingSettings Settings,
    IReadOnlyList<SavedCalculationRow> Rows,
    IReadOnlyList<string> Warnings)
{
    public static SavedCalculation Capture(Product product, PricingSettings settings, DateTime calculatedAtUtc)
    {
        var rows = product.CompositionRows.OrderBy(r => r.Position).ThenBy(r => r.Id)
            .Select(row => new SavedCalculationRow(
                row.Position,
                row.RowType,
                row.RowType == RowType.Material ? row.Material?.Name : row.Operation?.Name,
                row.RowType == RowType.Material ? row.Material?.Unit : null,
                row.Quantity,
                row.DimensionA,
                row.DimensionB,
                row.RowType == RowType.Material ? row.Material?.Price : row.Operation?.Rate,
                PriceCalculator.RowCost(row, settings)))
            .ToArray();

        return new SavedCalculation(
            product.Mode, calculatedAtUtc, PriceCalculator.Calculate(product, settings),
            new SavedPricingSettings(settings.AreaMaterialManual, settings.AreaMaterialAutomatic,
                settings.PieceMaterial, settings.MarginManual, settings.MarginAutomatic, settings.RoundingDecimals),
            rows, PriceWarnings.ForProduct(product).ToArray());
    }
}

public sealed record SavedCalculationRow(
    int Position, RowType Type, string? Name, string? Unit, decimal Quantity,
    decimal? DimensionA, decimal? DimensionB, decimal? InputPrice, decimal Cost);

public sealed record SavedPricingSettings(
    decimal AreaMaterialManual, decimal AreaMaterialAutomatic, decimal PieceMaterial,
    decimal MarginManual, decimal MarginAutomatic, int? RoundingDecimals);
