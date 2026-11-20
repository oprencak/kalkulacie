using System.Drawing.Drawing2D;
using Kalkulacie.Core.Calculations;
using Kalkulacie.Core.Enums;

namespace Kalkulacie.App.Forms;

/// <summary>
/// Prehľad koeficientov, s ktorými bola uložená kalkulácia výrobku vypočítaná: karta s dlaždicami
/// (prirážky v percentách ako v okne Koeficienty). Zobrazuje len koeficienty platné pre typ výroby výrobku.
/// </summary>
internal sealed class CoefficientsPanel : Control, ICustomPainted
{
    private static readonly Font TitleFont = new("Segoe UI Semibold", 9F);
    private static readonly Font LabelFont = new("Segoe UI", 8.25F);
    private static readonly Font ValueFont = new("Segoe UI Semibold", 12F);

    private const TextFormatFlags TextFlags = TextFormatFlags.Left | TextFormatFlags.SingleLine |
        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

    private sealed record Tile(string Label, string Value);

    private string _subtitle = "";
    private List<Tile> _tiles = [];

    public CoefficientsPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        AccessibleRole = AccessibleRole.StaticText;
        AccessibleName = "Koeficienty výpočtu";
    }

    /// <summary>Zobrazí koeficienty, s ktorými bola uložená kalkulácia vypočítaná.</summary>
    public void Display(SavedCalculation? calculation)
    {
        if (calculation is null)
        {
            _subtitle = "";
            _tiles = [];
        }
        else
        {
            bool manual = calculation.Mode == CalculationMode.Manual;
            var used = calculation.Settings;
            _subtitle = $"{(manual ? "Ručná" : "Strojová")} výroba · {calculation.CalculatedAtUtc.ToLocalTime():d.M.yyyy HH:mm}";
            _tiles =
            [
                new Tile("Plošný materiál – odpad", Percent(manual ? used.AreaMaterialManual : used.AreaMaterialAutomatic)),
                new Tile("Kusový materiál – odpad", Percent(used.PieceMaterial)),
                new Tile("Prirážka k UVN", Percent(manual ? used.MarginManual : used.MarginAutomatic)),
                new Tile("Zaokrúhľovanie", Rounding(used.RoundingDecimals)),
            ];
        }

        AccessibleDescription = _tiles.Count == 0
            ? ""
            : _subtitle + ". " + string.Join(". ", _tiles.Select(t => $"{t.Label}: {t.Value}"));
        Invalidate();
    }

    /// <summary>Koeficient ako prirážka v percentách (1,15 → „+15 %“), rovnako ako v okne Koeficienty.</summary>
    private static string Percent(decimal coefficient)
    {
        decimal percent = (coefficient - 1) * 100;
        string sign = percent > 0 ? "+" : percent < 0 ? "−" : "";
        return $"{sign}{Math.Abs(percent):0.####} %";
    }

    private static string Rounding(int? decimals) => decimals switch
    {
        null => "bez zaokrúhlenia",
        1 => "1 des. miesto",
        >= 2 and <= 4 => $"{decimals} des. miesta",
        _ => $"{decimals} des. miest",
    };

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.BackgroundOf(Parent));

        float scale = DeviceDpi / 96f;
        int S(float value) => (int)Math.Round(value * scale);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using (var path = Theme.RoundedRect(rect, 7f * scale))
        {
            using var fill = new SolidBrush(Theme.Surface);
            using var border = new Pen(Theme.Border);
            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }

        if (_tiles.Count == 0)
        {
            TextRenderer.DrawText(g, "Vyberte výrobok — zobrazia sa koeficienty, s ktorými bol vypočítaný.", LabelFont,
                ClientRectangle, Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            return;
        }

        int labelHeight = LabelFont.Height, valueHeight = ValueFont.Height;
        int blockHeight = labelHeight + S(1) + valueHeight;
        int top = Math.Max(S(4), (Height - blockHeight) / 2);
        int pad = S(14);

        // Nadpis vľavo: čo karta ukazuje, pre aký typ výroby a kedy sa počítalo
        int headingWidth = Math.Min(S(200), Width / 4);
        var heading = new Rectangle(pad, top, headingWidth - pad, TitleFont.Height);
        TextRenderer.DrawText(g, "Koeficienty výpočtu", TitleFont, heading, Theme.Text, Theme.Surface, TextFlags);
        heading.Offset(0, TitleFont.Height + S(3));
        heading.Height = labelHeight;
        TextRenderer.DrawText(g, _subtitle, LabelFont, heading, Theme.TextMuted, Theme.Surface, TextFlags);

        // Dlaždice: popis a hodnota
        float tileWidth = (Width - headingWidth) / (float)_tiles.Count;
        using var line = new Pen(Theme.GridLine);
        for (int i = 0; i < _tiles.Count; i++)
        {
            var tile = _tiles[i];
            int x = (int)(headingWidth + i * tileWidth);
            g.DrawLine(line, x, S(10), x, Height - S(10));

            var text = new Rectangle(x + pad, top, (int)tileWidth - pad - S(6), labelHeight);
            TextRenderer.DrawText(g, tile.Label, LabelFont, text, Theme.TextMuted, Theme.Surface, TextFlags);
            text.Offset(0, labelHeight + S(1));
            text.Height = valueHeight;
            TextRenderer.DrawText(g, tile.Value, ValueFont, text, Theme.Text, Theme.Surface, TextFlags);
        }
    }
}
