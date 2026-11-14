namespace Kalkulacie.App.Forms;

public class RowEditModel
{
    public int Id { get; set; }
    public string Typ { get; set; } = "";
    public string Položka { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal? DimensionA { get; set; }
    public decimal? DimensionB { get; set; }
    /// <summary>Jednotková cena materiálu alebo sadzba réžie.</summary>
    public decimal UnitPrice { get; set; }
    /// <summary>Náklad riadku podľa cenových vzorcov.</summary>
    public decimal Cost { get; set; }
}
