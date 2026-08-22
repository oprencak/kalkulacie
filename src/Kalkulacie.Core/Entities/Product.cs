using Kalkulacie.Core.Enums;

namespace Kalkulacie.Core.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public CalculationMode Mode { get; set; }

    public ICollection<CompositionRow> CompositionRows { get; set; } = [];
}
