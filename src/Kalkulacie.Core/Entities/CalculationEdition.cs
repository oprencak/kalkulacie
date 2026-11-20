using Kalkulacie.Core.Calculations;

namespace Kalkulacie.Core.Entities;

public class CalculationEdition
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateOnly Date { get; set; }
    public string Note { get; set; } = "";
    public int Revision { get; set; }
    /// <summary>Kľúče stĺpcov vypnutých pri tlači a exporte, oddelené čiarkou (prázdne = všetky stĺpce).</summary>
    public string HiddenColumns { get; set; } = "";
    public ICollection<CalculationEditionProduct> Products { get; set; } = [];
}

public class CalculationEditionProduct
{
    public int CalculationEditionId { get; set; }
    // Identita pôvodného výrobku slúži na prepočet. Uložená kalkulácia ostáva aj po jeho zmazaní.
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public SavedCalculation Calculation { get; set; } = null!;
}
