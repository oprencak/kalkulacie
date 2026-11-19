using Kalkulacie.Core.Entities;

namespace Kalkulacie.App.Forms;

internal sealed class CalculationEditionsForm : AppForm
{
    private readonly TextBox _search = new() { Width = 330, PlaceholderText = "Názov kalkulácie alebo výrobku" };
    private readonly DataGridView _grid = EditionControls.Grid(
        (nameof(EditionRow.Name), "Kalkulácia", 64, null),
        (nameof(EditionRow.Date), "Dátum", 21, null),
        (nameof(EditionRow.ProductCount), "Výrobky", 15, "0"));
    private readonly Button _open;
    private readonly Button _copy;
    private readonly Button _delete;
    private List<CalculationEditions.ListItem> _editions = [];

    private sealed record EditionRow(CalculationEditions.ListItem Edition)
    {
        public string Name => Edition.Name;
        public string Date => Edition.Date.ToString("d.M.yyyy");
        public int ProductCount => Edition.ProductNames.Count;
    }

    private CalculationEditions.ListItem? SelectedEdition => (_grid.CurrentRow?.DataBoundItem as EditionRow)?.Edition;

    public CalculationEditionsForm()
    {
        Text = "Uložené kalkulácie";
        ClientSize = new Size(750, 470);
        MinimumSize = new Size(570, 330);
        StartPosition = FormStartPosition.CenterParent;

        _open = EditionControls.Button("Otvoriť…", OpenSelected);
        _copy = EditionControls.Button("Vytvoriť kópiu…", CopySelected);
        _delete = EditionControls.Button("Zmazať…", DeleteSelected);
        var close = new Button { Text = "Zavrieť", AutoSize = true, DialogResult = DialogResult.Cancel };
        CancelButton = close;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(EditionControls.Bar(new Label { Text = "Hľadať", AutoSize = true, Margin = new Padding(3, 7, 8, 0) }, _search), 0, 0);
        layout.Controls.Add(_grid, 0, 1);
        layout.Controls.Add(EditionControls.Bar(EditionControls.Button("Nová kalkulácia…", NewEdition), _open, _copy, _delete, close), 0, 2);
        Controls.Add(layout);

        _search.TextChanged += (_, _) => ApplyFilter();
        _grid.SelectionChanged += (_, _) => UpdateButtons();
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenSelected(); };
        Load += (_, _) => Reload();
    }

    private void Reload(int? selectedId = null)
    {
        _editions = CalculationEditions.List();
        ApplyFilter(selectedId);
    }

    private void ApplyFilter(int? selectedId = null)
    {
        var term = _search.Text.Trim();
        var items = _editions.Where(e => TextSearch.Matches(e.Name, term) ||
                e.ProductNames.Any(p => TextSearch.Matches(p, term)))
            .Select(e => new EditionRow(e)).ToList();
        _grid.DataSource = items;
        int index = items.FindIndex(row => row.Edition.Id == selectedId);
        if (index >= 0)
            _grid.CurrentCell = _grid.Rows[index].Cells[0];
        UpdateButtons();
    }

    private void UpdateButtons() => _open.Enabled = _copy.Enabled = _delete.Enabled = SelectedEdition is not null;

    private void NewEdition() => Edit(new CalculationEdition { Date = DateOnly.FromDateTime(DateTime.Today) });

    private void OpenSelected()
    {
        if (SelectedEdition is { } selected)
            Edit(CalculationEditions.Open(selected.Id));
    }

    private void CopySelected()
    {
        if (SelectedEdition is not { } selected) return;
        var copy = CalculationEditions.Open(selected.Id);
        copy.Id = 0;
        copy.Revision = 0;
        copy.Name += " – kópia";
        copy.Date = DateOnly.FromDateTime(DateTime.Today);
        Edit(copy);
    }

    private void Edit(CalculationEdition edition)
    {
        using var editor = new CalculationEditionForm(edition);
        editor.ShowDialog(this);
        Reload(edition.Id);
    }

    private void DeleteSelected()
    {
        if (SelectedEdition is not { } selected) return;
        if (MessageBox.Show(this, $"Natrvalo zmazať kalkuláciu „{selected.Name}“?", "Zmazať kalkuláciu",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
        {
            CalculationEditions.Delete(selected);
            Reload();
        }
    }
}
