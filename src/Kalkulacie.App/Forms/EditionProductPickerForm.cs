using Kalkulacie.Core.Enums;

namespace Kalkulacie.App.Forms;

internal sealed class EditionProductPickerForm : AppForm
{
    private readonly List<CalculationEditions.ProductChoice> _catalog;
    private readonly HashSet<int> _selected = [];
    private readonly CheckedListBox _list = new() { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false, HorizontalScrollbar = true };
    private readonly TextBox _search = new() { Width = 270, PlaceholderText = "Hľadať výrobok" };
    private readonly ComboBox _mode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145 };
    private readonly Label _count = new() { AutoSize = true, Margin = new Padding(10, 7, 0, 0) };
    private readonly Button _add = new() { Text = "Pridať vybrané", AutoSize = true, DialogResult = DialogResult.OK };
    private bool _loading;

    private sealed record Item(CalculationEditions.ProductChoice Product)
    {
        public override string ToString() => $"{Product.Name} ({EditionControls.ModeLabel(Product.Mode)})";
    }

    public IReadOnlyCollection<int> SelectedIds => _selected;

    public EditionProductPickerForm(IReadOnlySet<int> existingIds)
    {
        _catalog = CalculationEditions.Catalog().Where(p => !existingIds.Contains(p.Id)).ToList();
        Text = "Pridať výrobky";
        ClientSize = new Size(610, 445);
        MinimumSize = new Size(550, 330);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;

        _mode.Items.AddRange(["Všetky výrobky", "Ručná výroba", "Strojová výroba"]);
        _mode.SelectedIndex = 0;
        var cancel = new Button { Text = "Zrušiť", AutoSize = true, DialogResult = DialogResult.Cancel };
        AcceptButton = _add;
        CancelButton = cancel;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(EditionControls.Bar(_search, _mode, _count), 0, 0);
        layout.Controls.Add(_list, 0, 1);
        layout.Controls.Add(EditionControls.Bar(EditionControls.Button("Označiť zobrazené", SelectVisible), _add, cancel), 0, 2);
        Controls.Add(layout);

        _search.TextChanged += (_, _) => ApplyFilter();
        _mode.SelectedIndexChanged += (_, _) => ApplyFilter();
        _list.ItemCheck += (_, e) =>
        {
            if (_loading || _list.Items[e.Index] is not Item item) return;
            if (e.NewValue == CheckState.Checked) _selected.Add(item.Product.Id);
            else _selected.Remove(item.Product.Id);
            UpdateCount();
        };
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var mode = _mode.SelectedIndex switch
        {
            1 => CalculationMode.Manual,
            2 => CalculationMode.Automatic,
            _ => (CalculationMode?)null,
        };
        var term = _search.Text.Trim();
        _loading = true;
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var product in _catalog.Where(p => (mode is null || mode == p.Mode) && TextSearch.Matches(p.Name, term)))
                _list.Items.Add(new Item(product), _selected.Contains(product.Id));
        }
        finally
        {
            _list.EndUpdate();
            _loading = false;
        }
        UpdateCount();
    }

    private void SelectVisible()
    {
        for (int i = 0; i < _list.Items.Count; i++)
            _list.SetItemChecked(i, true);
    }

    private void UpdateCount()
    {
        _count.Text = $"Vybrané: {_selected.Count}";
        _add.Enabled = _selected.Count > 0;
    }
}
