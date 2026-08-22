using Kalkulacie.Core.Enums;

namespace Kalkulacie.Core.Entities;

public class CompositionRow
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public RowType RowType { get; set; }

    // Materiálový riadok
    public int? MaterialId { get; set; }
    public Material? Material { get; set; }
    public decimal? DimensionA { get; set; }
    public decimal? DimensionB { get; set; }

    // Pracovný riadok
    public int? OperationId { get; set; }
    public Operation? Operation { get; set; }

    // Spoločné
    public decimal Quantity { get; set; }
}
