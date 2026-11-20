using System.Drawing.Printing;
using System.Text;
using Kalkulacie.Core;
using Kalkulacie.Core.Calculations;
using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kalkulacie.App.Forms;

public partial class PriceListForm : AppForm
{
    private const string ManualLabel = "Ručné";
    private const string AutomaticLabel = "Automat";

    private sealed record PriceListEntry(PriceListRow Row, IReadOnlyList<string> Warnings, DateTime? CalculatedAt = null);

    private readonly KalkulacieDbContext? _db;
    private readonly CalculationEdition? _edition;
    private List<PriceListEntry> _allRows = [];
    /// <summary>Riadky po filtri typu a hľadaní — to, čo je v gride, sa aj tlačí a exportuje.</summary>
    private List<PriceListEntry> _rows = [];

    /// <summary>
    /// Stĺpec výstupu. <c>Key</c> sa ukladá v <see cref="CalculationEdition.HiddenColumns"/> — nemeniť.
    /// <c>Header</c> je zároveň názov stĺpca v gride (vlastnosť <see cref="PriceListRow"/>).
    /// <c>CsvOnly</c> = stĺpec je len v CSV, nie v tabuľke, náhľade a tlači.
    /// </summary>
    private sealed record Column(string Key, string Header, float Weight, StringAlignment Align,
        Func<PriceListEntry, string, string> Value, bool CsvOnly = false);

    private const string NameKey = "name";

    private readonly List<Column> _columns =
    [
        new(NameKey,    "Výrobok",  3.5f, StringAlignment.Near, (e, _) => e.Row.Výrobok),
        new("type",     "Typ",      0.8f, StringAlignment.Near, (e, _) => e.Row.Typ),
        new("material", "Materiál", 1.0f, StringAlignment.Far,  (e, f) => e.Row.Materiál.ToString(f)),
        new("labor",    "Práca",    1.0f, StringAlignment.Far,  (e, f) => e.Row.Práca.ToString(f)),
        new("uvn",      "UVN",      1.0f, StringAlignment.Far,  (e, f) => e.Row.UVN.ToString(f)),
        new("price",    "Cena",     1.0f, StringAlignment.Far,  (e, f) => e.Row.Cena.ToString(f)),
    ];

    /// <summary>Kľúče vypnutých stĺpcov; pri uloženej kalkulácii sa pamätajú v DB, v cenníku len do zatvorenia okna.</summary>
    private readonly HashSet<string> _hidden = [];

    private int _printRowIndex;
    private int _printPageNumber;
    private DateTime _printTime;

    public PriceListForm() : this(null) { }

    internal PriceListForm(CalculationEdition? edition)
    {
        InitializeComponent();
        _edition = edition;
        if (edition is null)
        {
            _db = AppDb.CreateContext();
        }
        else
        {
            Text = Title;
            btnRefresh.Visible = false;
            int offset = btnPreview.Left - btnRefresh.Left;
            btnPreview.Left -= offset;
            btnPrint.Left -= offset;
            btnExport.Left -= offset;
            btnColumns.Left -= offset;

            _columns.Add(new("edition", "Kalkulácia", 0, StringAlignment.Near, (_, _) => edition.Name, CsvOnly: true));
            _columns.Add(new("editionDate", "Dátum kalkulácie", 0, StringAlignment.Near, (_, _) => EditionDate, CsvOnly: true));
            _columns.Add(new("calculatedAt", "Dátum výpočtu", 0, StringAlignment.Near,
                (e, _) => $"{e.CalculatedAt:d.M.yyyy HH:mm}", CsvOnly: true));
            _hidden.UnionWith(edition.HiddenColumns.Split(',', StringSplitOptions.RemoveEmptyEntries));
            _hidden.Remove(NameKey);
        }
        Load += OnLoad;
    }

    private void OnLoad(object? sender, EventArgs e) => RefreshGrid();

    private string NumberFormat => $"N{(int)numDecimals.Value}";
    private string Title => _edition is null ? "Kalkulácie" : _edition.Name;
    private string EditionDate => _edition?.Date.ToString("d.M.yyyy") ?? "";
    private string OutputContext => _edition is null ? AppDb.DbName : $"{AppDb.DbName} — dátum kalkulácie {EditionDate}";

    private string FilterLabel =>
        rdManual.Checked ? "ručná výroba" : rdAutomatic.Checked ? "strojová výroba" : "všetky výrobky";

    private void RefreshGrid()
    {
        if (_edition is not null)
        {
            _allRows = _edition.Products.OrderBy(p => p.Calculation.Mode)
                .ThenBy(p => p.ProductName, StringComparer.CurrentCultureIgnoreCase)
                .Select(p => CreateEntry(p.ProductName, p.Calculation.Mode, p.Calculation.Summary,
                    p.Calculation.Warnings, p.Calculation.CalculatedAtUtc.ToLocalTime()))
                .ToList();
        }
        else if (_db is not null)
        {
            _db.ChangeTracker.Clear();
            var settings = _db.GetPricingSettings();
            _allRows = _db.Products
                .Include(p => p.CompositionRows).ThenInclude(r => r.Material)
                .Include(p => p.CompositionRows).ThenInclude(r => r.Operation)
                .AsEnumerable().OrderBy(p => p.Mode)
                .ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(p => CreateEntry(p.Name, p.Mode, PriceCalculator.Calculate(p, settings), PriceWarnings.ForProduct(p)))
                .ToList();
        }

        ApplyFilter();
    }

    private static PriceListEntry CreateEntry(string name, CalculationMode mode, PriceSummary summary,
        IEnumerable<string> warnings, DateTime? calculatedAt = null)
    {
        var type = mode == CalculationMode.Manual ? ManualLabel : AutomaticLabel;
        var row = new PriceListRow(name, type, summary.MaterialTotal, summary.OperationTotal, summary.Uvn, summary.BasePrice);
        return new PriceListEntry(row, warnings.Select(w => $"{name} ({type}) · {w}").ToArray(), calculatedAt);
    }

    private void ApplyFilter()
    {
        var term = txtSearch.Text.Trim();
        _rows = _allRows
            .Where(r => !rdManual.Checked || r.Row.Typ == ManualLabel)
            .Where(r => !rdAutomatic.Checked || r.Row.Typ == AutomaticLabel)
            .Where(r => TextSearch.Matches(r.Row.Výrobok, term))
            .ToList();

        grid.DataSource = _rows.Select(r => r.Row).ToList();
        lblCount.Text = $"Počet: {_rows.Count}";
        ApplyFormat();
    }

    private void ApplyFormat()
    {
        if (grid.Columns.Count == 0) return;

        foreach (DataGridViewColumn col in grid.Columns)
        {
            if (col.Name is "Materiál" or "Práca" or "UVN" or "Cena")
            {
                col.DefaultCellStyle.Format = NumberFormat;
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }
        foreach (var column in _columns.Where(c => !c.CsvOnly))
            grid.Columns[column.Header]!.Visible = !_hidden.Contains(column.Key);
        grid.Columns["Výrobok"]!.FillWeight = 300;
        grid.Invalidate();
    }

    /// <summary>Zapnuté stĺpce pre tabuľku, náhľad a tlač; s <paramref name="csv"/> aj stĺpce, ktoré sú len v CSV.</summary>
    private List<Column> OutputColumns(bool csv = false) =>
        _columns.Where(c => !_hidden.Contains(c.Key) && (csv || !c.CsvOnly)).ToList();

    private void btnColumns_Click(object sender, EventArgs e)
    {
        var menu = new ContextMenuStrip { ShowCheckMargin = true, ShowImageMargin = false };
        foreach (var column in _columns.Where(c => c.Key != NameKey))
        {
            var item = new ToolStripMenuItem(column.CsvOnly ? $"{column.Header} (len v CSV)" : column.Header)
            {
                Checked = !_hidden.Contains(column.Key), CheckOnClick = true,
            };
            item.CheckedChanged += (_, _) => SetColumnVisible(column, item.Checked);
            menu.Items.Add(item);
        }
        // Ponuka ostáva otvorená, aby sa dalo zaškrtnúť viac stĺpcov naraz.
        menu.Closing += (_, args) => args.Cancel = args.CloseReason == ToolStripDropDownCloseReason.ItemClicked;
        menu.Closed += (_, _) => BeginInvoke(menu.Dispose);
        menu.Show(btnColumns, new Point(0, -menu.GetPreferredSize(Size.Empty).Height));
    }

    private void SetColumnVisible(Column column, bool visible)
    {
        if (visible) _hidden.Remove(column.Key);
        else _hidden.Add(column.Key);
        ApplyFormat();

        if (_edition is null) return;
        _edition.HiddenColumns = string.Join(",", _columns.Select(c => c.Key).Where(_hidden.Contains));
        CalculationEditions.SaveHiddenColumns(_edition);
    }

    private void rdFilter_CheckedChanged(object sender, EventArgs e)
    {
        if (((RadioButton)sender).Checked)
            ApplyFilter();
    }

    private void txtSearch_TextChanged(object sender, EventArgs e) => ApplyFilter();

    private void numDecimals_ValueChanged(object sender, EventArgs e) => ApplyFormat();

    private void btnRefresh_Click(object sender, EventArgs e) => RefreshGrid();

    private void btnPreview_Click(object sender, EventArgs e)
    {
        var path = Path.Combine(Path.GetTempPath(), _edition is null ? "kalkulacie_cennik.html" : $"ulozena_kalkulacia_{_edition.Id}.html");
        File.WriteAllText(path, GenerateHtml(), Encoding.UTF8);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void btnPrint_Click(object sender, EventArgs e)
    {
        using var doc = new PrintDocument { DocumentName = Title };
        doc.BeginPrint += (_, _) =>
        {
            _printRowIndex = 0;
            _printPageNumber = 0;
            _printTime = DateTime.Now;
        };
        doc.PrintPage += OnPrintPage;

        using var dlg = new PrintDialog { Document = doc, UseEXDialog = false };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        doc.Print();
    }

    private void btnExport_Click(object sender, EventArgs e)
    {
        if (!ConfirmExport()) return;

        using var dlg = new SaveFileDialog
        {
            Title = "Export cenníka",
            Filter = "CSV pre Excel (*.csv)|*.csv",
            FileName = _edition is null ? $"kalkulacie_{DateTime.Now:yyyy-MM-dd}.csv" : $"ulozena_kalkulacia_{_edition.Date:yyyy-MM-dd}.csv",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            File.WriteAllLines(dlg.FileName, CsvLines(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Súbor sa nepodarilo uložiť:\n{ex.Message}", "Chyba",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private IEnumerable<string> CsvLines()
    {
        // Bez oddeľovača tisícov, aby Excel hodnoty čítal ako čísla.
        var format = _edition is null ? $"F{(int)numDecimals.Value}" : EditionControls.DecimalFormat;
        var columns = OutputColumns(csv: true);
        yield return string.Join(";", columns.Select(c => CsvField(c.Header)));

        foreach (var entry in _rows)
            yield return string.Join(";", columns.Select(c => CsvField(c.Value(entry, format))));
    }

    private bool ConfirmExport()
    {
        var warnings = _rows.SelectMany(r => r.Warnings).ToArray();
        if (warnings.Length == 0) return true;

        using var dlg = new AppForm
        {
            Text = "Kontrola kalkulácií pred exportom",
            StartPosition = FormStartPosition.CenterParent,
            ShowInTaskbar = false,
            MinimizeBox = false,
            ClientSize = new Size(760, 330),
            MinimumSize = new Size(520, 260),
        };
        var label = new Label
        {
            Text = $"Počet upozornení vo vybranom cenníku: {warnings.Length}",
            Location = new Point(12, 12), AutoSize = true,
        };
        var list = new ListBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Location = new Point(12, 38), Size = new Size(736, 236),
            IntegralHeight = false, HorizontalScrollbar = true,
        };
        list.Items.AddRange(warnings);
        var proceed = new Button
        {
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Text = "Pokračovať v exporte", Location = new Point(474, 286), Size = new Size(150, 32),
            DialogResult = DialogResult.OK,
        };
        var cancel = new Button
        {
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Text = "Zrušiť export", Location = new Point(632, 286), Size = new Size(116, 32),
            DialogResult = DialogResult.Cancel,
        };
        dlg.AcceptButton = cancel;
        dlg.CancelButton = cancel;
        dlg.Controls.AddRange([label, list, proceed, cancel]);
        dlg.ActiveControl = cancel;
        return dlg.ShowDialog(this) == DialogResult.OK;
    }

    private static string CsvField(string value) =>
        value.IndexOfAny([';', '"', '\r', '\n']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;

    private string GenerateHtml()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<!DOCTYPE html><html><head><meta charset='utf-8'><title>{Encode(Title)}</title>");
        sb.AppendLine("<style>body{font-family:Arial,sans-serif;font-size:12px;margin:20px}" +
            "h2{margin-bottom:4px}p{margin:0 0 10px;color:#555}table{width:100%;border-collapse:collapse}" +
            "th,td{border:1px solid #ccc;padding:4px 8px}th{background:#eee}" +
            ".r{text-align:right}</style></head><body>");
        sb.AppendLine($"<h2>{Encode(Title)}</h2>");
        sb.AppendLine($"<p>{Encode(OutputContext)} — {Encode(FilterLabel)} — " +
            $"{DateTime.Now:d.M.yyyy H:mm} — počet: {_rows.Count}</p>");
        var columns = OutputColumns();
        sb.AppendLine("<table><thead><tr>" +
            string.Concat(columns.Select(c => $"<th>{Encode(c.Header)}</th>")) +
            "</tr></thead><tbody>");

        foreach (var r in _rows)
        {
            sb.Append("<tr>");
            foreach (var c in columns)
                sb.Append(c.Align == StringAlignment.Far
                    ? $"<td class='r'>{c.Value(r, NumberFormat)}</td>"
                    : $"<td>{Encode(c.Value(r, NumberFormat))}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");
        return sb.ToString();
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);

    private void OnPrintPage(object sender, PrintPageEventArgs e)
    {
        var g = e.Graphics!;
        var bounds = e.MarginBounds;
        _printPageNumber++;

        var columns = OutputColumns();
        float totalWeight = columns.Sum(c => c.Weight);
        float[] colWidths = columns.Select(c => c.Weight / totalWeight * bounds.Width).ToArray();

        using var headerFont = new Font("Segoe UI", 8F, FontStyle.Bold);
        using var rowFont = new Font("Segoe UI", 8F);
        using var titleFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        using var rightAligned = new StringFormat { Alignment = StringAlignment.Far };

        // Päta na každej strane: dátum tlače vľavo, číslo strany vpravo
        float footerH = rowFont.GetHeight(g) + 6;
        float footerY = bounds.Bottom - rowFont.GetHeight(g);
        g.DrawString($"{OutputContext} — {FilterLabel} — {_printTime:d.M.yyyy H:mm}",
            rowFont, Brushes.Gray, bounds.Left, footerY);
        g.DrawString($"Strana {_printPageNumber}", rowFont, Brushes.Gray,
            new RectangleF(bounds.Left, footerY, bounds.Width, footerH), rightAligned);
        float contentBottom = bounds.Bottom - footerH;

        float y = bounds.Top;

        // Nadpis len na prvej strane
        if (_printPageNumber == 1)
        {
            float titleHeight = Math.Min(g.MeasureString(Title, titleFont, bounds.Width).Height, titleFont.GetHeight(g) * 2);
            using var titleFormat = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
            g.DrawString(Title, titleFont, Brushes.Black, new RectangleF(bounds.Left, y, bounds.Width, titleHeight), titleFormat);
            y += titleHeight + 8;
        }

        // Hlavička stĺpcov
        float rowH = headerFont.GetHeight(g) + 4;
        float x = bounds.Left;
        for (int i = 0; i < columns.Count; i++)
        {
            using var fmt = new StringFormat { Alignment = columns[i].Align, LineAlignment = StringAlignment.Center };
            var rect = new RectangleF(x, y, colWidths[i], rowH);
            g.FillRectangle(Brushes.LightGray, rect);
            g.DrawString(columns[i].Header, headerFont, Brushes.Black, rect, fmt);
            x += colWidths[i];
        }
        y += rowH;

        // Riadky dát
        rowH = rowFont.GetHeight(g) + 3;
        bool hasMore = false;
        string numberFormat = NumberFormat;

        while (_printRowIndex < _rows.Count)
        {
            if (y + rowH > contentBottom)
            {
                hasMore = true;
                break;
            }

            var entry = _rows[_printRowIndex];

            x = bounds.Left;
            for (int i = 0; i < columns.Count; i++)
            {
                using var fmt = new StringFormat
                {
                    Alignment = columns[i].Align,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap,
                };
                var rect = new RectangleF(x + 2, y, colWidths[i] - 4, rowH);
                g.DrawString(columns[i].Value(entry, numberFormat), rowFont, Brushes.Black, rect, fmt);
                x += colWidths[i];
            }

            // Oddeľovacia čiara
            g.DrawLine(Pens.LightGray, bounds.Left, y + rowH, bounds.Right, y + rowH);
            y += rowH;
            _printRowIndex++;
        }

        e.HasMorePages = hasMore;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _db?.Dispose();
        base.OnFormClosed(e);
    }
}
