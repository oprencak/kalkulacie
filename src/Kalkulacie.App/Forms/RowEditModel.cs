namespace Kalkulacie.App.Forms;

public class RowEditModel
{
    public int Id { get; set; }
    public string Typ { get; set; } = "";
    public string Položka { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal? DimensionA { get; set; }
    public decimal? DimensionB { get; set; }
}
