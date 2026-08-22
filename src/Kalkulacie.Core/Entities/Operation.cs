namespace Kalkulacie.Core.Entities;

public class Operation
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Rate { get; set; }

    public ICollection<CompositionRow> CompositionRows { get; set; } = [];
}
