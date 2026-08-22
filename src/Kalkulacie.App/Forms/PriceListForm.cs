using System.Drawing.Printing;
using Kalkulacie.Core;
using Kalkulacie.Core.Calculations;
using Kalkulacie.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kalkulacie.App.Forms;

public partial class PriceListForm : Form
{
    private readonly KalkulacieDbContext _db;
    private List<PriceListRow> _rows = [];

    // stĺpce: (hlavička, šírka v jednotkách, zarovnanie)
    private static readonly (string Header, float Weight, StringAlignment Align)[] Columns =
    [
        ("Výrobok",   3.5f, StringAlignment.Near),
        ("Typ",       0.8f, StringAlignment.Near),
        ("Materiál",  1.0f, StringAlignment.Far),
        ("Práca",     1.0f, StringAlignment.Far),
        ("UVN",       1.0f, StringAlignment.Far),
        ("Cena",      1.0f, StringAlignment.Far),
        ("Cena +8%",  1.0f, StringAlignment.Far),
        ("Cena +16%", 1.0f, StringAlignment.Far),
    ];

    private int _printPageIndex;

    public PriceListForm()
    {
        InitializeComponent();
        _db = AppDb.CreateContext();
        Load += OnLoad;
    }

    private void OnLoad(object? sender, EventArgs e) => RefreshGrid();

    private void RefreshGrid()
    {
        var products = _db.Products
            .Include(p => p.CompositionRows).ThenInclude(r => r.Material)
            .Include(p => p.CompositionRows).ThenInclude(r => r.Operation)
            .OrderBy(p => p.Mode).ThenBy(p => p.Name)
            .ToList();

        _rows = products.Select(p =>
        {
            var s = PriceCalculator.Calculate(p);
            return new PriceListRow(
                p.Name,
                p.Mode == CalculationMode.Manual ? "Ručné" : "Automat",
                s.MaterialTotal, s.OperationTotal, s.Uvn,
                s.BasePrice, s.Price1, s.Price2);
        }).ToList();

        grid.DataSource = _rows;

        if (grid.Columns.Count > 0)
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.Name is "Materiál" or "Práca" or "UVN" or "Cena" or "Cena8" or "Cena16")
                    col.DefaultCellStyle.Format = "N4";
            }
            grid.Columns["Cena8"].HeaderText = "Cena +8%";
            grid.Columns["Cena16"].HeaderText = "Cena +16%";
        }
    }

    private void btnRefresh_Click(object sender, EventArgs e) => RefreshGrid();

    private void btnPreview_Click(object sender, EventArgs e)
    {
        var path = Path.Combine(Path.GetTempPath(), "kalkulacie_cennik.html");
        File.WriteAllText(path, GenerateHtml(), System.Text.Encoding.UTF8);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void btnPrint_Click(object sender, EventArgs e)
    {
        _printPageIndex = 0;
        var doc = new PrintDocument { DocumentName = "Kalkulacie" };
        doc.PrintPage += OnPrintPage;

        using var dlg = new PrintDialog { Document = doc, UseEXDialog = false };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        doc.Print();
    }

    private string GenerateHtml()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Kalkulacie</title>");
        sb.AppendLine("<style>body{font-family:Arial,sans-serif;font-size:12px;margin:20px}" +
            "h2{margin-bottom:8px}table{width:100%;border-collapse:collapse}" +
            "th,td{border:1px solid #ccc;padding:4px 8px}th{background:#eee}" +
            ".r{text-align:right}@media print{button{display:none}}</style></head><body>");
        sb.AppendLine("<h2>Kalkulacie</h2>");
        sb.AppendLine("<table><thead><tr><th>Vyrobok</th><th>Typ</th><th>Material</th>" +
            "<th>Praca</th><th>UVN</th><th>Cena</th><th>Cena +8%</th><th>Cena +16%</th></tr></thead><tbody>");

        foreach (var r in _rows)
        {
            sb.AppendLine($"<tr><td>{Encode(r.Výrobok)}</td><td>{Encode(r.Typ)}</td>" +
                $"<td class='r'>{r.Materiál:N4}</td><td class='r'>{r.Práca:N4}</td>" +
                $"<td class='r'>{r.UVN:N4}</td><td class='r'>{r.Cena:N4}</td>" +
                $"<td class='r'>{r.Cena8:N4}</td><td class='r'>{r.Cena16:N4}</td></tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");
        return sb.ToString();
    }

    private static string Encode(string s) => System.Net.WebUtility.HtmlEncode(s);

    private void OnPrintPage(object sender, PrintPageEventArgs e)
    {
        var g = e.Graphics!;
        var bounds = e.MarginBounds;

        float totalWeight = Columns.Sum(c => c.Weight);
        float[] colWidths = Columns.Select(c => c.Weight / totalWeight * bounds.Width).ToArray();

        using var headerFont = new Font("Segoe UI", 8F, FontStyle.Bold);
        using var rowFont = new Font("Segoe UI", 8F);
        using var titleFont = new Font("Segoe UI", 12F, FontStyle.Bold);

        float y = bounds.Top;

        // Nadpis len na prvej strane
        if (_printPageIndex == 0)
        {
            g.DrawString("Kalkulácie", titleFont, Brushes.Black, bounds.Left, y);
            y += titleFont.GetHeight(g) + 8;
        }

        // Hlavička stĺpcov
        float rowH = headerFont.GetHeight(g) + 4;
        float x = bounds.Left;
        for (int i = 0; i < Columns.Length; i++)
        {
            var fmt = new StringFormat { Alignment = Columns[i].Align, LineAlignment = StringAlignment.Center };
            var rect = new RectangleF(x, y, colWidths[i], rowH);
            g.FillRectangle(Brushes.LightGray, rect);
            g.DrawString(Columns[i].Header, headerFont, Brushes.Black, rect, fmt);
            x += colWidths[i];
        }
        y += rowH;

        // Riadky dát
        rowH = rowFont.GetHeight(g) + 3;
        bool hasMore = false;

        while (_printPageIndex < _rows.Count)
        {
            if (y + rowH > bounds.Bottom)
            {
                hasMore = true;
                break;
            }

            var row = _rows[_printPageIndex];
            string[] values =
            [
                row.Výrobok, row.Typ,
                row.Materiál.ToString("N4"), row.Práca.ToString("N4"),
                row.UVN.ToString("N4"), row.Cena.ToString("N4"),
                row.Cena8.ToString("N4"), row.Cena16.ToString("N4"),
            ];

            x = bounds.Left;
            for (int i = 0; i < Columns.Length; i++)
            {
                var fmt = new StringFormat { Alignment = Columns[i].Align, LineAlignment = StringAlignment.Center };
                var rect = new RectangleF(x + 2, y, colWidths[i] - 4, rowH);
                g.DrawString(values[i], rowFont, Brushes.Black, rect, fmt);
                x += colWidths[i];
            }

            // Oddeľovacia čiara
            g.DrawLine(Pens.LightGray, bounds.Left, y + rowH, bounds.Right, y + rowH);
            y += rowH;
            _printPageIndex++;
        }

        e.HasMorePages = hasMore;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _db.Dispose();
        base.OnFormClosed(e);
    }
}
