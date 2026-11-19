using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;

namespace Kalkulacie.App.Forms;

/// <summary>
/// Spoločný vzhľad aplikácie: farby a štýl tlačidiel a tabuliek. Aplikuje ho <see cref="AppForm"/> pri načítaní
/// okna na všetky ovládacie prvky, takže formuláre samy nič nastavovať nemusia.
/// Veľkosť písma sa nemení — rozloženie okien je počítané na Segoe UI 9.
/// </summary>
internal static class Theme
{
    public static readonly Color Window = Color.FromArgb(244, 246, 248);
    public static readonly Color Surface = Color.White;
    public static readonly Color SurfaceAlt = Color.FromArgb(249, 250, 251);
    public static readonly Color Text = Color.FromArgb(31, 41, 55);
    public static readonly Color TextMuted = Color.FromArgb(107, 114, 128);
    public static readonly Color TextDisabled = Color.FromArgb(160, 166, 176);
    public static readonly Color Border = Color.FromArgb(205, 210, 218);
    public static readonly Color GridLine = Color.FromArgb(230, 233, 238);
    public static readonly Color Accent = Color.FromArgb(14, 124, 107);
    public static readonly Color AccentHover = Color.FromArgb(11, 106, 91);
    public static readonly Color AccentPressed = Color.FromArgb(8, 88, 76);
    public static readonly Color AccentDark = Color.FromArgb(10, 84, 74);
    public static readonly Color AccentSoft = Color.FromArgb(214, 238, 232);
    public static readonly Color Hover = Color.FromArgb(238, 241, 245);
    public static readonly Color Pressed = Color.FromArgb(225, 229, 235);
    public static readonly Color Disabled = Color.FromArgb(236, 238, 241);
    public static readonly Color WarningBack = Color.FromArgb(255, 244, 206);
    public static readonly Color WarningBorder = Color.FromArgb(240, 215, 140);

    private static readonly ConditionalWeakTable<Button, object> PrimaryButtons = new();
    private static readonly ConditionalWeakTable<Control, object> Styled = new();

    /// <summary>Označí hlavné tlačidlo okna (plná farba). Tlačidlo <c>AcceptButton</c> je hlavné automaticky.</summary>
    public static T Primary<T>(T button) where T : Button
    {
        PrimaryButtons.AddOrUpdate(button, button);
        button.Invalidate();
        return button;
    }

    public static void Apply(Form form)
    {
        form.BackColor = Window;
        form.ForeColor = Text;
        ApplyTo(form.Controls);
    }

    private static void ApplyTo(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            if (control is ICustomPainted || !Styled.TryAdd(control, control))
                continue;

            switch (control)
            {
                case Button button:
                    StyleButton(button);
                    break;
                case DataGridView grid:
                    StyleGrid(grid);
                    break;
                // Zoznamy s vodorovným posúvaním ostávajú systémové — šírku obsahu si počíta Windows
                case ListBox { HorizontalScrollbar: false } list when list.GetType() == typeof(ListBox):
                    StyleList(list);
                    break;
                case TextBox { ReadOnly: true } textBox:
                    textBox.BackColor = SurfaceAlt;
                    break;
            }
            ApplyTo(control.Controls);
        }
    }

    // ── Tlačidlá ─────────────────────────────────────────────────────────────

    private static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.UseVisualStyleBackColor = false;
        if (button.AutoSize)
        {
            // Tlačidlá s automatickou veľkosťou by boli po zmene štýlu natesno okolo textu
            button.Padding = new Padding(10, 2, 10, 2);
            button.MinimumSize = new Size(84, 29);
        }
        button.Paint += PaintButton;
    }

    private static void PaintButton(object? sender, PaintEventArgs e)
    {
        var button = (Button)sender!;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackgroundOf(button.Parent));

        bool primary = PrimaryButtons.TryGetValue(button, out _) || ReferenceEquals(button.FindForm()?.AcceptButton, button);
        bool hover = button.ClientRectangle.Contains(button.PointToClient(Cursor.Position));
        bool pressed = hover && Control.MouseButtons == MouseButtons.Left && button.Capture;

        Color fill, border, text;
        if (!button.Enabled)
            (fill, border, text) = (Disabled, GridLine, TextDisabled);
        else if (primary)
            (fill, border, text) = (pressed ? AccentPressed : hover ? AccentHover : Accent, Color.Empty, Color.White);
        else
            (fill, border, text) = (pressed ? Pressed : hover ? Hover : Surface, button.Focused ? Accent : Border, Text);

        var rect = new RectangleF(0.5f, 0.5f, button.Width - 1, button.Height - 1);
        float radius = 5f * button.DeviceDpi / 96f;
        using (var path = RoundedRect(rect, radius))
        {
            using var brush = new SolidBrush(fill);
            g.FillPath(brush, path);
            if (!border.IsEmpty)
            {
                using var pen = new Pen(border);
                g.DrawPath(pen, path);
            }
        }

        if (primary && button.Focused && button.Enabled)
        {
            // Zameranie klávesnicou na plnom tlačidle: tenký svetlý rámik vnútri
            using var inner = RoundedRect(RectangleF.Inflate(rect, -2.5f, -2.5f), Math.Max(1f, radius - 2f));
            using var pen = new Pen(Color.FromArgb(190, Color.White));
            g.DrawPath(pen, inner);
        }

        TextRenderer.DrawText(g, button.Text, button.Font, button.ClientRectangle, text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis);
    }

    /// <summary>Farba pozadia pod ovládacím prvkom (prvý nepriehľadný predok).</summary>
    public static Color BackgroundOf(Control? control)
    {
        for (; control is not null; control = control.Parent)
        {
            if (control.BackColor.A == 255)
                return control.BackColor;
        }
        return Window;
    }

    public static GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        float d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    // ── Zoznamy ──────────────────────────────────────────────────────────────

    /// <summary>Zoznam s výberom vo farbe aplikácie (rovnakej ako v tabuľkách) a vzdušnejšími riadkami.</summary>
    private static void StyleList(ListBox list)
    {
        list.DrawMode = DrawMode.OwnerDrawFixed;
        list.ItemHeight = list.Font.Height + 6;
        list.DrawItem += (_, e) =>
        {
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using var back = new SolidBrush(selected ? AccentSoft : Surface);
            e.Graphics.FillRectangle(back, e.Bounds);
            if (e.Index < 0) return;

            var bounds = Rectangle.FromLTRB(e.Bounds.Left + 4, e.Bounds.Top, e.Bounds.Right - 2, e.Bounds.Bottom);
            TextRenderer.DrawText(e.Graphics, list.GetItemText(list.Items[e.Index]), e.Font ?? list.Font, bounds,
                list.Enabled ? Text : TextDisabled,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        };
    }

    // ── Tabuľky ──────────────────────────────────────────────────────────────

    private static void StyleGrid(DataGridView grid)
    {
        int textHeight = grid.Font.Height;

        // Vlastný tenký rámik namiesto systémového: kreslí ho rodič tesne okolo tabuľky,
        // takže ho neovplyvní posúvanie obsahu.
        grid.BorderStyle = BorderStyle.None;
        if (grid.Parent is { } parent)
        {
            parent.Paint += (_, e) =>
            {
                if (!grid.Visible) return;
                using var pen = new Pen(Border);
                e.Graphics.DrawRectangle(pen, grid.Left - 1, grid.Top - 1, grid.Width + 1, grid.Height + 1);
            };
            grid.Resize += (_, _) => parent.Invalidate();
            grid.Move += (_, _) => parent.Invalidate();
        }

        grid.BackgroundColor = Surface;
        grid.GridColor = GridLine;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.RowHeadersVisible = false;
        grid.AllowUserToResizeRows = false;
        grid.RowTemplate.Height = textHeight + 12;
        foreach (DataGridViewRow row in grid.Rows) // tabuľka naplnená ešte pred zobrazením okna
            row.Height = grid.RowTemplate.Height;

        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = textHeight + 18;

        var header = grid.ColumnHeadersDefaultCellStyle;
        header.BackColor = header.SelectionBackColor = Color.FromArgb(238, 241, 245);
        header.ForeColor = header.SelectionForeColor = Color.FromArgb(55, 65, 81);
        header.Font = new Font("Segoe UI Semibold", grid.Font.SizeInPoints);
        header.Padding = new Padding(4, 0, 4, 0);
        header.WrapMode = DataGridViewTriState.False;

        var cell = grid.DefaultCellStyle;
        cell.BackColor = Surface;
        cell.ForeColor = Text;
        cell.SelectionBackColor = AccentSoft;
        cell.SelectionForeColor = Text;
        cell.Padding = new Padding(4, 0, 4, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = SurfaceAlt;

        // Hlavička stĺpca sa zarovnáva rovnako ako jeho hodnoty (čísla vpravo). Zarovnanie stĺpcov
        // nastavujú formuláre až po naplnení dát, preto sa sleduje zmena štýlu.
        grid.CellStyleContentChanged += (_, e) =>
        {
            if ((e.CellStyleScope & DataGridViewCellStyleScopes.Column) != 0)
                AlignHeaders(grid);
        };
        grid.ColumnAdded += (_, _) => AlignHeaders(grid);
        AlignHeaders(grid);
    }

    private static void AlignHeaders(DataGridView grid)
    {
        foreach (DataGridViewColumn column in grid.Columns)
        {
            var alignment = column.DefaultCellStyle.Alignment == DataGridViewContentAlignment.MiddleRight
                ? DataGridViewContentAlignment.MiddleRight
                : DataGridViewContentAlignment.MiddleLeft;
            if (column.HeaderCell.Style.Alignment != alignment)
                column.HeaderCell.Style.Alignment = alignment;
        }
    }
}

/// <summary>Ovládací prvok, ktorý sa kreslí sám — <see cref="Theme"/> ho neštýluje.</summary>
internal interface ICustomPainted;
