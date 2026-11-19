using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Kalkulacie.App.Forms;

/// <summary>
/// Položka hlavného menu: karta s ikonou, názvom (<see cref="Control.Text"/>) a krátkym popisom.
/// Je to obyčajné tlačidlo (klávesnica, Enter/medzerník), len sa kreslí samo.
/// </summary>
internal sealed class NavButton : Button, ICustomPainted
{
    private static readonly Font TitleFont = new("Segoe UI Semibold", 10.5F);
    private static readonly Font DescriptionFont = new("Segoe UI", 8.25F);
    private static readonly Font GlyphFont = new("Segoe MDL2 Assets", 15F);
    private static readonly Font ChevronFont = new("Segoe MDL2 Assets", 8F);

    private bool _hover;
    private bool _pressed;

    /// <summary>Popis pod názvom.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Description { get; set; } = "";

    /// <summary>Znak z písma Segoe MDL2 Assets (súčasť Windows 10 a 11).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Glyph { get; set; } = "";

    public NavButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        AccessibleRole = AccessibleRole.PushButton;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = e.Button == MouseButtons.Left; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(Theme.BackgroundOf(Parent));

        float scale = DeviceDpi / 96f;
        bool active = _hover || Focused;
        var rect = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using (var path = Theme.RoundedRect(rect, 7f * scale))
        {
            using var fill = new SolidBrush(_pressed ? Theme.AccentSoft : _hover ? Color.FromArgb(240, 249, 247) : Theme.Surface);
            using var border = new Pen(active ? Theme.Accent : Theme.Border);
            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }

        // Ikona v zaoblenom štvorci vľavo
        float box = Height - 16f * scale;
        var iconRect = new RectangleF(8f * scale, (Height - box) / 2f, box, box);
        using (var path = Theme.RoundedRect(iconRect, 6f * scale))
        using (var fill = new SolidBrush(Theme.AccentSoft))
            g.FillPath(fill, path);

        using var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var accent = new SolidBrush(Theme.AccentDark);
        g.DrawString(Glyph, GlyphFont, accent, iconRect, center);

        // Šípka vpravo
        float chevronWidth = 26f * scale;
        using var muted = new SolidBrush(active ? Theme.Accent : Theme.TextDisabled);
        g.DrawString("", ChevronFont, muted, new RectangleF(Width - chevronWidth - 2f * scale, 0, chevronWidth, Height), center);

        // Názov a popis
        int textX = (int)(iconRect.Right + 12f * scale);
        int textWidth = (int)(Width - chevronWidth - 2f * scale) - textX;
        const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
                                      TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        int titleHeight = TitleFont.Height, descriptionHeight = DescriptionFont.Height;
        int y = (Height - titleHeight - descriptionHeight) / 2 - 1;
        TextRenderer.DrawText(g, Text, TitleFont, new Rectangle(textX, y, textWidth, titleHeight), Theme.Text, flags);
        TextRenderer.DrawText(g, Description, DescriptionFont,
            new Rectangle(textX, y + titleHeight, textWidth, descriptionHeight), Theme.TextMuted, flags);
    }
}
