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

    private sealed record PriceListEntry(PriceListRow Row, IReadOnlyList<string> Warnings);

    private readonly KalkulacieDbContext _db;
    private List<PriceListEntry> _allRows = [];
    /// <summary>Riadky po filtri typu a hľadaní — to, čo je v gride, sa aj tlačí a exportuje.</summary>
    private List<PriceListEntry> _rows = [];

    private PricingSettings _settings = new();

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

    public PriceListForm()
    {
        InitializeComponent();
        _db = AppDb.CreateContext();
        Load += OnLoad;
    }

    private void OnLoad(object? sender, EventArgs e) => RefreshGrid();

    private string NumberFormat => $"N{(int)numDecimals.Value}";

    private string FilterLabel =>
        rdManual.Checked ? "ručná výroba" : rdAutomatic.Checked ? "strojová výroba" : "všetky výrobky";

    private void RefreshGrid()
    {
        _db.ChangeTracker.Clear();
        _settings = _db.GetPricingSettings();
        var products = _db.Products
            .Include(p => p.CompositionRows).ThenInclude(r => r.Material)
            .Include(p => p.CompositionRows).ThenInclude(r => r.Operation)
            .ToList();

        _allRows = products
            .OrderBy(p => p.Mode)
            .ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(p =>
            {
                var s = PriceCalculator.Calculate(p, _settings);
                var row = new PriceListRow(
                    p.Name,
                    p.Mode == CalculationMode.Manual ? ManualLabel : AutomaticLabel,
                    s.MaterialTotal, s.OperationTotal, s.Uvn,
                    s.BasePrice);
                return new PriceListEntry(row, GetWarnings(p, row.Typ).ToList());
            }).ToList();

        ApplyFilter();
    }

    private static IEnumerable<string> GetWarnings(Product product, string type)
    {
        var label = $"{product.Name} ({type})";
        if (product.CompositionRows.Count == 0)
            yield return $"{label} · výrobok bez zloženia";

        foreach (var row in product.CompositionRows.Where(r => r.RowType == RowType.Material).OrderBy(r => r.Position))
        {
            var materialLabel = $"{label} · riadok {row.Position} ({row.Material?.Name ?? "materiál"})";
            if (row.Material?.Price == 0)
                yield return $"{materialLabel} · nulová cena materiálu";

            // Kusový riadok má oba rozmery prázdne. Jeden vyplnený rozmer signalizuje plošný riadok.
            if ((row.DimensionA.HasValue || row.DimensionB.HasValue) &&
                (row.DimensionA.GetValueOrDefault() <= 0 || row.DimensionB.GetValueOrDefault() <= 0))
            {
                yield return $"{materialLabel} · chýbajúci alebo neplatný rozmer plošného materiálu";
            }
        }
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
        var path = Path.Combine(Path.GetTempPath(), "kalkulacie_cennik.html");
        File.WriteAllText(path, GenerateHtml(), Encoding.UTF8);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void btnPrint_Click(object sender, EventArgs e)
    {
        var doc = new PrintDocument { DocumentName = "Kalkulácie" };
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
            FileName = $"kalkulacie_{DateTime.Now:yyyy-MM-dd}.csv",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        // Bodkočiarka + čísla v miestnom formáte + UTF-8 s BOM: slovenský Excel súbor otvorí priamo.
        // Bez oddeľovača tisícov, aby Excel hodnoty čítal ako čísla.
        var numberFormat = $"F{(int)numDecimals.Value}";
        var lines = new List<string> { string.Join(";", Columns.Select(c => CsvField(c.Header))) };
        lines.AddRange(_rows.Select(r => string.Join(";", FormatValues(r.Row, numberFormat).Select(CsvField))));

        try
        {
            File.WriteAllLines(dlg.FileName, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Súbor sa nepodarilo uložiť:\n{ex.Message}", "Chyba",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Kalkulácie</title>");
        sb.AppendLine("<style>body{font-family:Arial,sans-serif;font-size:12px;margin:20px}" +
            "h2{margin-bottom:4px}p{margin:0 0 10px;color:#555}table{width:100%;border-collapse:collapse}" +
            "th,td{border:1px solid #ccc;padding:4px 8px}th{background:#eee}" +
            ".r{text-align:right}</style></head><body>");
        sb.AppendLine("<h2>Kalkulácie</h2>");
        sb.AppendLine($"<p>{Encode(AppDb.DbName)} — {Encode(FilterLabel)} — " +
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
        g.DrawString($"{AppDb.DbName} — {FilterLabel} — {_printTime:d.M.yyyy H:mm}",
            rowFont, Brushes.Gray, bounds.Left, footerY);
        g.DrawString($"Strana {_printPageNumber}", rowFont, Brushes.Gray,
            new RectangleF(bounds.Left, footerY, bounds.Width, footerH), rightAligned);
        float contentBottom = bounds.Bottom - footerH;

        float y = bounds.Top;

        // Nadpis len na prvej strane
        if (_printPageNumber == 1)
        {
            g.DrawString("Kalkulácie", titleFont, Brushes.Black, bounds.Left, y);
            y += titleFont.GetHeight(g) + 8;
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
        _db.Dispose();
        base.OnFormClosed(e);
    }
}
