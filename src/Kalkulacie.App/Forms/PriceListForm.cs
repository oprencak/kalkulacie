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

    // stĺpce: (hlavička, šírka v jednotkách, zarovnanie)
    private static readonly (string Header, float Weight, StringAlignment Align)[] Columns =
    [
        ("Výrobok",   3.5f, StringAlignment.Near),
        ("Typ",       0.8f, StringAlignment.Near),
        ("Materiál",  1.0f, StringAlignment.Far),
        ("Práca",     1.0f, StringAlignment.Far),
        ("UVN",       1.0f, StringAlignment.Far),
        ("Cena",      1.0f, StringAlignment.Far),
    ];

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
        grid.Columns["Výrobok"]!.FillWeight = 300;
        grid.Invalidate();
    }

    private void rdFilter_CheckedChanged(object sender, EventArgs e)
    {
        if (((RadioButton)sender).Checked)
            ApplyFilter();
    }

    private void txtSearch_TextChanged(object sender, EventArgs e) => ApplyFilter();

    private void numDecimals_ValueChanged(object sender, EventArgs e) => ApplyFormat();

    private void btnRefresh_Click(object sender, EventArgs e) => RefreshGrid();

    private string[] FormatValues(PriceListRow row, string numberFormat) =>
    [
        row.Výrobok, row.Typ,
        row.Materiál.ToString(numberFormat), row.Práca.ToString(numberFormat),
        row.UVN.ToString(numberFormat), row.Cena.ToString(numberFormat),
    ];

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
        IEnumerable<string> headers = Columns.Select(c => c.Header);
        if (_edition is not null) headers = headers.Concat(["Kalkulácia", "Dátum kalkulácie", "Dátum výpočtu"]);
        yield return string.Join(";", headers.Select(CsvField));

        foreach (var entry in _rows)
        {
            IEnumerable<string> values = FormatValues(entry.Row, format);
            if (_edition is not null)
                values = values.Concat([_edition.Name, EditionDate, $"{entry.CalculatedAt:d.M.yyyy HH:mm}"]);
            yield return string.Join(";", values.Select(CsvField));
        }
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
        sb.AppendLine("<table><thead><tr>" +
            string.Concat(Columns.Select(c => $"<th>{Encode(c.Header)}</th>")) +
            "</tr></thead><tbody>");

        foreach (var r in _rows)
        {
            var values = FormatValues(r.Row, NumberFormat);
            sb.Append("<tr>");
            for (int i = 0; i < values.Length; i++)
                sb.Append(Columns[i]!.Align == StringAlignment.Far
                    ? $"<td class='r'>{values[i]}</td>"
                    : $"<td>{Encode(values[i])}</td>");
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

        float totalWeight = Columns.Sum(c => c.Weight);
        float[] colWidths = Columns.Select(c => c.Weight / totalWeight * bounds.Width).ToArray();

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
        for (int i = 0; i < Columns.Length; i++)
        {
            using var fmt = new StringFormat { Alignment = Columns[i]!.Align, LineAlignment = StringAlignment.Center };
            var rect = new RectangleF(x, y, colWidths[i], rowH);
            g.FillRectangle(Brushes.LightGray, rect);
            g.DrawString(Columns[i]!.Header, headerFont, Brushes.Black, rect, fmt);
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

            string[] values = FormatValues(_rows[_printRowIndex].Row, numberFormat);

            x = bounds.Left;
            for (int i = 0; i < Columns.Length; i++)
            {
                using var fmt = new StringFormat
                {
                    Alignment = Columns[i]!.Align,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap,
                };
                var rect = new RectangleF(x + 2, y, colWidths[i] - 4, rowH);
                g.DrawString(values[i], rowFont, Brushes.Black, rect, fmt);
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
