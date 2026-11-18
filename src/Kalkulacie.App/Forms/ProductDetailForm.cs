using Kalkulacie.Core;
using Kalkulacie.Core.Calculations;
using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kalkulacie.App.Forms;

public partial class ProductDetailForm : AppForm
{
    private readonly KalkulacieDbContext _db;
    private readonly int _productId;
    private Product _product = null!;
    private PricingSettings _settings = new();
    private BindingSource _rowsSource = new();

    public ProductDetailForm(int productId)
    {
        InitializeComponent();
        _productId = productId;
        _db = AppDb.CreateContext();
        Load += OnLoad;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        _product = _db.Products
            .Include(p => p.CompositionRows)
                .ThenInclude(r => r.Material)
            .Include(p => p.CompositionRows)
                .ThenInclude(r => r.Operation)
            .Single(p => p.Id == _productId);

        _settings = _db.GetPricingSettings();

        txtName.Text = _product.Name;
        cmbMode.DataSource = Enum.GetValues<CalculationMode>();
        cmbMode.SelectedItem = _product.Mode;
        cmbMode.SelectedIndexChanged += OnModeChanged;
        Text = $"Výrobok — {_product.Name}";

        RefreshGrid();
        RefreshSummary();
    }

    private void RefreshGrid()
    {
        var rows = OrderedRows().Select(r => new RowEditModel
        {
            Id = r.Id,
            Typ = r.RowType == RowType.Material ? "Materiál" : "Práca",
            Položka = (r.RowType == RowType.Material ? r.Material?.Name : r.Operation?.Name) ?? "",
            Unit = (r.RowType == RowType.Material ? r.Material?.Unit : null) ?? "",
            Quantity = r.Quantity,
            DimensionA = r.DimensionA,
            DimensionB = r.DimensionB,
            UnitPrice = (r.RowType == RowType.Material ? r.Material?.Price : r.Operation?.Rate) ?? 0,
            Cost = PriceCalculator.RowCost(r, _settings),
        }).ToList();

        int position = _rowsSource.Position;
        _rowsSource.DataSource = rows;
        gridRows.DataSource = _rowsSource;
        if (position >= 0 && rows.Count > 0)
            _rowsSource.Position = Math.Min(position, rows.Count - 1);

        if (gridRows.Columns.Count > 0)
        {
            gridRows.Columns["Id"]!.Visible = false;
            gridRows.Columns["Typ"]!.HeaderText = "Typ";
            gridRows.Columns["Položka"]!.HeaderText = "Položka";
            gridRows.Columns["Quantity"]!.HeaderText = "Množstvo / počet ks";
            gridRows.Columns["DimensionA"]!.HeaderText = "Rozmer A";
            gridRows.Columns["DimensionB"]!.HeaderText = "Rozmer B";
            gridRows.Columns["Unit"]!.HeaderText = "MJ";
            gridRows.Columns["UnitPrice"]!.HeaderText = "Cena / sadzba";
            gridRows.Columns["Cost"]!.HeaderText = "Náklad";
            gridRows.Columns["Položka"]!.FillWeight = 250;
            gridRows.Columns["Unit"]!.FillWeight = 50;

            foreach (var name in new[] { "Quantity", "DimensionA", "DimensionB" })
            {
                gridRows.Columns[name]!.DefaultCellStyle.Format = "0.####";
                gridRows.Columns[name]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            foreach (var name in new[] { "UnitPrice", "Cost" })
            {
                gridRows.Columns[name]!.DefaultCellStyle.Format = "N4";
                gridRows.Columns[name]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }
    }

    private List<CompositionRow> OrderedRows() =>
        _product.CompositionRows.OrderBy(r => r.Position).ThenBy(r => r.Id).ToList();

    private int NextPosition() =>
        _product.CompositionRows.Select(r => r.Position).DefaultIfEmpty(0).Max() + 1;

    private void RefreshSummary()
    {
        var summary = PriceCalculator.Calculate(_product, _settings);
        lblMaterialTotal.Text = $"Materiál:  {summary.MaterialTotal:N4}";
        lblOperationTotal.Text = $"Práca:     {summary.OperationTotal:N4}";
        lblUvn.Text = $"UVN:       {summary.Uvn:N4}";
        lblBasePrice.Text = $"Cena:      {summary.BasePrice:N4}";
    }

    // Všetko v tomto okne sa ukladá hneď: riadky zloženia, typ výrobku (zmenou v zozname)
    // aj názov (tlačidlom Uložiť alebo najneskôr pri zatvorení okna).

    private void OnModeChanged(object? sender, EventArgs e)
    {
        _product.Mode = (CalculationMode)cmbMode.SelectedItem!;
        _db.SaveChanges();
        RefreshGrid();
        RefreshSummary();
    }

    /// <summary>Uloží názov, ak sa zmenil. Vráti false, ak je prázdny.</summary>
    private bool SaveName()
    {
        var name = txtName.Text.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show("Výrobok musí mať názov.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtName.Focus();
            return false;
        }

        if (name != _product.Name)
        {
            _product.Name = name;
            _db.SaveChanges();
        }
        Text = $"Výrobok — {_product.Name}";
        return true;
    }

    private void btnSave_Click(object sender, EventArgs e) => SaveName();

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_product is not null && !SaveName())
            e.Cancel = true;
        base.OnFormClosing(e);
    }

    private void btnAddMaterial_Click(object sender, EventArgs e)
    {
        using var dlg = new MaterialRowForm(_db);
        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedMaterial == null) return;

        var row = new CompositionRow
        {
            ProductId = _product.Id,
            Product = _product,
            RowType = RowType.Material,
            Position = NextPosition(),
            MaterialId = dlg.SelectedMaterial.Id,
            Material = dlg.SelectedMaterial,
            Quantity = dlg.Quantity,
            DimensionA = dlg.DimensionA,
            DimensionB = dlg.DimensionB,
        };
        _product.CompositionRows.Add(row);
        _db.SaveChanges();
        RefreshGrid();
        RefreshSummary();
    }

    private void btnAddOperation_Click(object sender, EventArgs e)
    {
        using var dlg = new OperationRowForm(_db);
        if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedOperation == null) return;

        var row = new CompositionRow
        {
            ProductId = _product.Id,
            Product = _product,
            RowType = RowType.Operation,
            Position = NextPosition(),
            OperationId = dlg.SelectedOperation.Id,
            Operation = dlg.SelectedOperation,
            Quantity = dlg.Quantity,
        };
        _product.CompositionRows.Add(row);
        _db.SaveChanges();
        RefreshGrid();
        RefreshSummary();
    }

    private void btnEditRow_Click(object sender, EventArgs e) => EditCurrentRow();

    private void gridRows_DoubleClick(object sender, EventArgs e) => EditCurrentRow();

    private void EditCurrentRow()
    {
        if (_rowsSource.Current is not RowEditModel model) return;
        var row = _product.CompositionRows.FirstOrDefault(r => r.Id == model.Id);
        if (row == null) return;

        if (row.RowType == RowType.Material)
        {
            using var dlg = new MaterialRowForm(_db, row);
            if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedMaterial == null) return;
            row.MaterialId = dlg.SelectedMaterial.Id;
            row.Material = dlg.SelectedMaterial;
            row.Quantity = dlg.Quantity;
            row.DimensionA = dlg.DimensionA;
            row.DimensionB = dlg.DimensionB;
        }
        else
        {
            using var dlg = new OperationRowForm(_db, row);
            if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedOperation == null) return;
            row.OperationId = dlg.SelectedOperation.Id;
            row.Operation = dlg.SelectedOperation;
            row.Quantity = dlg.Quantity;
        }

        _db.SaveChanges();
        RefreshGrid();
        RefreshSummary();
    }

    private void btnMoveUp_Click(object sender, EventArgs e) => MoveCurrentRow(-1);

    private void btnMoveDown_Click(object sender, EventArgs e) => MoveCurrentRow(+1);

    /// <summary>Posunie vybraný riadok o jedno miesto a prečísluje poradie celého zloženia.</summary>
    private void MoveCurrentRow(int direction)
    {
        if (_rowsSource.Current is not RowEditModel model) return;

        var ordered = OrderedRows();
        int index = ordered.FindIndex(r => r.Id == model.Id);
        int target = index + direction;
        if (index < 0 || target < 0 || target >= ordered.Count) return;

        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (int i = 0; i < ordered.Count; i++)
            ordered[i].Position = i + 1;

        _db.SaveChanges();
        RefreshGrid();
        _rowsSource.Position = target;
    }

    private void btnDeleteRow_Click(object sender, EventArgs e)
    {
        if (_rowsSource.Current is not RowEditModel model) return;
        var row = _product.CompositionRows.FirstOrDefault(r => r.Id == model.Id);
        if (row == null) return;

        if (MessageBox.Show($"Vymazať riadok '{model.Položka}' zo zloženia?", "Potvrdiť vymazanie",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _product.CompositionRows.Remove(row);
        _db.CompositionRows.Remove(row);
        _db.SaveChanges();
        RefreshGrid();
        RefreshSummary();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _db.Dispose();
        base.OnFormClosed(e);
    }
}
