namespace Kalkulacie.Core.Entities;

public class Material
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Unit { get; set; }
    public decimal Price { get; set; }

    public ICollection<CompositionRow> CompositionRows { get; set; } = [];
}
