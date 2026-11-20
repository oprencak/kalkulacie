using Kalkulacie.Core.Calculations;
using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;

namespace Kalkulacie.App.Forms;

internal sealed class CalculationEditionForm : AppForm
{
    private readonly CalculationEdition _edition;
    private readonly TextBox _name = new() { Width = 370 };
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short, Width = 125 };
    private readonly TextBox _note = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly CoefficientsPanel _coefficients = new() { Dock = DockStyle.Fill, Margin = new Padding(3, 6, 3, 3) };
    private readonly Label _warnings = new()
    {
        AutoSize = true, Visible = false, BackColor = Theme.WarningBack, Padding = new Padding(8, 5, 8, 5),
        Margin = new Padding(3, 6, 3, 0),
    };
    private readonly DataGridView _products = EditionControls.Grid(
        (nameof(ProductRow.Name), "Výrobok", 26, null), (nameof(ProductRow.Mode), "Výroba", 10, null),
        (nameof(ProductRow.Material), "Materiál", 11, EditionControls.DecimalFormat),
        (nameof(ProductRow.Labor), "Práca", 11, EditionControls.DecimalFormat),
        (nameof(ProductRow.Uvn), "UVN", 11, EditionControls.DecimalFormat),
        (nameof(ProductRow.Price), "Cena", 12, EditionControls.DecimalFormat),
        (nameof(ProductRow.CalculatedAt), "Dátum výpočtu", 19, null));
    private readonly DataGridView _composition = EditionControls.Grid(
        (nameof(SavedCalculationRow.Position), "Poradie", 7, "0"), (nameof(DetailRow.Type), "Typ", 10, null),
        (nameof(SavedCalculationRow.Name), "Položka", 22, null), (nameof(SavedCalculationRow.Unit), "MJ", 7, null),
        (nameof(SavedCalculationRow.Quantity), "Množstvo / ks", 13, EditionControls.DecimalFormat),
        (nameof(SavedCalculationRow.DimensionA), "Rozmer A", 10, EditionControls.DecimalFormat),
        (nameof(SavedCalculationRow.DimensionB), "Rozmer B", 10, EditionControls.DecimalFormat),
        (nameof(SavedCalculationRow.InputPrice), "Cena / sadzba", 12, EditionControls.DecimalFormat),
        (nameof(SavedCalculationRow.Cost), "Náklad", 10, EditionControls.DecimalFormat));

    private readonly Button _remove;
    private readonly Button _recalculateSelected;
    private readonly Button _recalculateAll;
    private readonly Button _output;
    private bool _dirty;

    private sealed record ProductRow(CalculationEditionProduct Product)
    {
        public string Name => Product.ProductName;
        public string Mode => EditionControls.ModeLabel(Product.Calculation.Mode);
        public decimal Material => Product.Calculation.Summary.MaterialTotal;
        public decimal Labor => Product.Calculation.Summary.OperationTotal;
        public decimal Uvn => Product.Calculation.Summary.Uvn;
        public decimal Price => Product.Calculation.Summary.BasePrice;
        public string CalculatedAt => Product.Calculation.CalculatedAtUtc.ToLocalTime().ToString("d.M.yyyy HH:mm");
    }

    private sealed record DetailRow(int Position, string Type, string? Name, string? Unit, decimal Quantity,
        decimal? DimensionA, decimal? DimensionB, decimal? InputPrice, decimal Cost);

    private sealed record PriceChange(string Product, string Mode, decimal Previous, decimal? New, decimal? Difference, string Status);

    private CalculationEditionProduct? SelectedProduct => (_products.CurrentRow?.DataBoundItem as ProductRow)?.Product;

    public CalculationEditionForm(CalculationEdition edition)
    {
        _edition = edition;
        Text = edition.Name.Length == 0 ? "Nová kalkulácia" : $"Uložená kalkulácia — {edition.Name}";
        ClientSize = new Size(1050, 680);
        MinimumSize = new Size(830, 540);
        StartPosition = FormStartPosition.CenterParent;

        _name.Text = edition.Name;
        _date.Value = edition.Date.ToDateTime(TimeOnly.MinValue);
        _note.Text = edition.Note;

        _remove = EditionControls.Button("Odobrať vybraný", RemoveSelected);
        _recalculateSelected = EditionControls.Button("Prepočítať vybraný…", () =>
        {
            if (SelectedProduct is { } product) Recalculate([product]);
        });
        _recalculateAll = EditionControls.Button("Prepočítať všetky…", () => Recalculate(_edition.Products));
        _output = EditionControls.Button("Tlač a export…", OpenOutput);
        var save = Theme.Primary(EditionControls.Button("Uložiť kalkuláciu", Save));
        var close = EditionControls.Button("Zavrieť", Close);
        CancelButton = close;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 9, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(EditionControls.Bar(FieldLabel("Názov kalkulácie"), _name, FieldLabel("Dátum"), _date), 0, 0);
        var noteLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        noteLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        noteLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        noteLayout.Controls.Add(FieldLabel("Poznámka"), 0, 0);
        noteLayout.Controls.Add(_note, 1, 0);
        layout.Controls.Add(noteLayout, 0, 1);
        layout.Controls.Add(_products, 0, 2);
        layout.Controls.Add(EditionControls.Bar(EditionControls.Button("Pridať výrobky…", AddProducts), _remove,
            _recalculateSelected, _recalculateAll), 0, 3);
        layout.Controls.Add(FieldLabel("Rozpis vybraného výrobku"), 0, 4);
        layout.Controls.Add(_composition, 0, 5);
        layout.Controls.Add(_coefficients, 0, 6);
        layout.Controls.Add(_warnings, 0, 7);
        layout.Controls.Add(EditionControls.Bar(save, _output, close), 0, 8);
        Controls.Add(layout);

        _name.TextChanged += (_, _) => MarkDirty();
        _date.ValueChanged += (_, _) => MarkDirty();
        _note.TextChanged += (_, _) => MarkDirty();
        _products.SelectionChanged += (_, _) => ShowProductDetail();
        Load += (_, _) => RefreshProducts();
    }

    private static Label FieldLabel(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(3, 7, 8, 3) };

    private void RefreshProducts(int? selectedId = null)
    {
        var items = _edition.Products.OrderBy(p => p.ProductName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(p => p.Calculation.Mode).Select(p => new ProductRow(p)).ToList();
        _products.DataSource = items;
        int index = items.FindIndex(p => p.Product.ProductId == selectedId);
        if (index >= 0) _products.CurrentCell = _products.Rows[index].Cells[0];
        ShowProductDetail();
    }

    private void ShowProductDetail()
    {
        var calculation = SelectedProduct?.Calculation;
        _remove.Enabled = _recalculateSelected.Enabled = calculation is not null;
        _recalculateAll.Enabled = _edition.Products.Count > 0;
        _output.Enabled = _edition.Id != 0 && !_dirty && _edition.Products.Count > 0;
        _composition.DataSource = calculation?.Rows.Select(r => new DetailRow(r.Position,
            r.Type == RowType.Material ? "Materiál" : "Práca", r.Name, r.Unit, r.Quantity, r.DimensionA,
            r.DimensionB, r.InputPrice, r.Cost)).ToList();

        _coefficients.Display(calculation);
        _warnings.Text = calculation is null ? "" : string.Join(Environment.NewLine, calculation.Warnings.Select(w => $"Upozornenie: {w}"));
        _warnings.Visible = _warnings.Text.Length > 0;
    }

    private void AddProducts()
    {
        using var picker = new EditionProductPickerForm(_edition.Products.Select(p => p.ProductId).ToHashSet());
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        var products = CalculationEditions.Calculate(picker.SelectedIds);
        if (products.Count < picker.SelectedIds.Count)
            MessageBox.Show(this, "Niektoré vybrané výrobky už nie sú v databáze.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        if (products.Count == 0) return;

        foreach (var product in products)
            _edition.Products.Add(product);
        MarkDirty();
        RefreshProducts(products[0].ProductId);
    }

    private void RemoveSelected()
    {
        if (SelectedProduct is not { } product) return;
        _edition.Products.Remove(product);
        MarkDirty();
        RefreshProducts();
    }

    private void Recalculate(ICollection<CalculationEditionProduct> products)
    {
        var replacements = CalculationEditions.Calculate(products.Select(p => p.ProductId)).ToDictionary(p => p.ProductId);
        var changes = products.OrderBy(p => p.ProductName, StringComparer.CurrentCultureIgnoreCase).Select(product =>
        {
            replacements.TryGetValue(product.ProductId, out var replacement);
            var before = product.Calculation.Summary.BasePrice;
            var after = replacement?.Calculation.Summary.BasePrice;
            var status = replacement is null ? "Chýba v databáze" : $"Upozornenia: {replacement.Calculation.Warnings.Count}";
            return new PriceChange(product.ProductName, EditionControls.ModeLabel(product.Calculation.Mode), before, after, after - before, status);
        }).ToList();
        if (!ConfirmRecalculation(changes, replacements.Count > 0)) return;

        foreach (var product in products)
        {
            if (!replacements.TryGetValue(product.ProductId, out var replacement)) continue;
            product.ProductName = replacement.ProductName;
            product.Calculation = replacement.Calculation;
        }
        MarkDirty();
        RefreshProducts(SelectedProduct?.ProductId);
    }

    private bool ConfirmRecalculation(List<PriceChange> changes, bool canApply)
    {
        using var dialog = new AppForm
        {
            Text = "Porovnanie pred prepočtom", StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(810, 340), MinimumSize = new Size(650, 260), ShowInTaskbar = false,
        };
        var grid = EditionControls.Grid(
            (nameof(PriceChange.Product), "Výrobok", 27, null), (nameof(PriceChange.Mode), "Výroba", 12, null),
            (nameof(PriceChange.Previous), "Pôvodná cena", 13, EditionControls.DecimalFormat),
            (nameof(PriceChange.New), "Nová cena", 13, EditionControls.DecimalFormat),
            (nameof(PriceChange.Difference), "Rozdiel", 12, EditionControls.DecimalFormat),
            (nameof(PriceChange.Status), "Kontrola", 23, null));
        grid.DataSource = changes;
        var apply = new Button { Text = "Použiť prepočet", AutoSize = true, DialogResult = DialogResult.OK, Enabled = canApply };
        var cancel = new Button { Text = canApply ? "Zrušiť" : "Zavrieť", AutoSize = true, DialogResult = DialogResult.Cancel };
        dialog.CancelButton = cancel;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(FieldLabel("Pri chýbajúcich výrobkoch sa zachová uložená kalkulácia."), 0, 0);
        layout.Controls.Add(grid, 0, 1);
        layout.Controls.Add(EditionControls.Bar(apply, cancel), 0, 2);
        dialog.Controls.Add(layout);
        dialog.Shown += (_, _) => cancel.Focus();
        return dialog.ShowDialog(this) == DialogResult.OK;
    }

    private void MarkDirty()
    {
        _dirty = true;
        _output.Enabled = false;
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            MessageBox.Show(this, "Zadajte názov kalkulácie.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            _name.Focus();
            return;
        }

        _edition.Name = _name.Text;
        _edition.Date = DateOnly.FromDateTime(_date.Value);
        _edition.Note = _note.Text;
        CalculationEditions.Save(_edition);
        _name.Text = _edition.Name;
        _dirty = false;
        ShowProductDetail();
        Text = $"Uložená kalkulácia — {_edition.Name}";
    }

    private void OpenOutput()
    {
        using var output = new PriceListForm(_edition);
        output.ShowDialog(this);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_dirty && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = MessageBox.Show(this, "Zavrieť kalkuláciu bez uloženia zmien?", Text, MessageBoxButtons.YesNo,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes;
        }
        base.OnFormClosing(e);
    }
}
