using Kalkulacie.Core.Calculations;
using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;

namespace Kalkulacie.App.Forms;

/// <summary>Zoznam výrobkov, v ktorých sa používa daný materiál alebo réžia.</summary>
public class UsageForm : AppForm
{
    private record UsageRow(
        string Product, string Mode, decimal Quantity, decimal? DimensionA, decimal? DimensionB,
        decimal RowCost, decimal ProductPrice);

    /// <summary>
    /// Zobrazí použitie položky. Výrobky musia mať načítané celé zloženie (materiály aj réžie),
    /// <paramref name="isMatch"/> vyberá riadky patriace hľadanej položke.
    /// </summary>
    public static void ShowFor(IWin32Window owner, string itemName,
        IReadOnlyCollection<Product> products, Func<CompositionRow, bool> isMatch, PricingSettings settings)
    {
        if (products.Count == 0)
        {
            MessageBox.Show(owner, $"'{itemName}' sa nepoužíva v žiadnom výrobku.", "Kde sa používa",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new UsageForm(itemName, products, isMatch, settings);
        form.ShowDialog(owner);
    }

    private UsageForm(string itemName, IReadOnlyCollection<Product> products, Func<CompositionRow, bool> isMatch,
        PricingSettings settings)
    {
        var rows = products
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .SelectMany(p =>
            {
                var price = PriceCalculator.Calculate(p, settings).BasePrice;
                return p.CompositionRows.Where(isMatch).Select(r => new UsageRow(
                    p.Name,
                    p.Mode == CalculationMode.Manual ? "Ručné" : "Automat",
                    r.Quantity, r.DimensionA, r.DimensionB,
                    PriceCalculator.RowCost(r, settings), price));
            })
            .ToList();

        Text = $"Kde sa používa — {itemName}";
        ClientSize = new Size(820, 460);
        MinimumSize = new Size(500, 300);
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly = true,
            AllowUserToAddRows = false,
        };
        var lblCount = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Text = $"Počet výrobkov: {products.Count}",
        };
        Controls.Add(grid);
        Controls.Add(lblCount);

        Load += (_, _) =>
        {
            grid.DataSource = rows;
            SetColumn(grid, nameof(UsageRow.Product), "Výrobok", null, 250);
            SetColumn(grid, nameof(UsageRow.Mode), "Typ", null);
            SetColumn(grid, nameof(UsageRow.Quantity), "Množstvo", "0.####");
            SetColumn(grid, nameof(UsageRow.DimensionA), "Rozmer A", "0.####");
            SetColumn(grid, nameof(UsageRow.DimensionB), "Rozmer B", "0.####");
            SetColumn(grid, nameof(UsageRow.RowCost), "Náklad riadku", "N4");
            SetColumn(grid, nameof(UsageRow.ProductPrice), "Cena výrobku", "N4");
        };
    }

    private static void SetColumn(DataGridView grid, string name, string header, string? format, int fillWeight = 100)
    {
        var col = grid.Columns[name];
        col.HeaderText = header;
        col.FillWeight = fillWeight;
        if (format is null) return;
        col.DefaultCellStyle.Format = format;
        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
    }
}
