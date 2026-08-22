using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;

namespace Kalkulacie.Core.Calculations;

public static class PriceCalculator
{
    public static decimal MaterialCost(CompositionRow row, decimal unitPrice)
    {
        if (row.DimensionA.HasValue && row.DimensionB.HasValue
            && row.DimensionA.Value != 0 && row.DimensionB.Value != 0)
        {
            // Plošný materiál (fólia/plech)
            decimal coefficient = row.Product.Mode == CalculationMode.Manual ? 1.15m : 1.05m;
            return row.DimensionA.Value * row.DimensionB.Value * coefficient * unitPrice * row.Quantity;
        }

        // Kusový materiál
        return unitPrice * row.Quantity * 1.03m;
    }

    public static decimal OperationCost(decimal rate, decimal quantity)
    {
        if (quantity == 0) return 0;
        return rate / quantity;
    }

    public static PriceSummary Calculate(Product product)
    {
        decimal materialTotal = 0;
        decimal operationTotal = 0;

        foreach (var row in product.CompositionRows)
        {
            if (row.RowType == RowType.Material && row.Material != null)
                materialTotal += MaterialCost(row, row.Material.Price);
            else if (row.RowType == RowType.Operation && row.Operation != null)
                operationTotal += OperationCost(row.Operation.Rate, row.Quantity);
        }

        decimal uvn = materialTotal + operationTotal;
        decimal profitCoefficient = product.Mode == CalculationMode.Manual ? 1.15m : 1.08m;
        decimal basePrice = uvn * profitCoefficient;

        return new PriceSummary(
            MaterialTotal: materialTotal,
            OperationTotal: operationTotal,
            Uvn: uvn,
            BasePrice: basePrice,
            Price1: basePrice * 1.08m,
            Price2: basePrice * 1.16m
        );
    }
}

public record PriceSummary(
    decimal MaterialTotal,
    decimal OperationTotal,
    decimal Uvn,
    decimal BasePrice,
    decimal Price1,
    decimal Price2
);
