using Kalkulacie.Core;
using Kalkulacie.Core.Calculations;
using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kalkulacie.App.Forms;

public partial class ProductDetailForm : Form
{
    private readonly KalkulacieDbContext _db;
    private readonly int _productId;
    private Product _product = null!;
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

        txtName.Text = _product.Name;
        cmbMode.DataSource = Enum.GetValues<CalculationMode>();
        cmbMode.SelectedItem = _product.Mode;

        RefreshGrid();
        RefreshSummary();
    }

    private void RefreshGrid()
    {
        var rows = _product.CompositionRows.Select(r => new RowEditModel
        {
            Id = r.Id,
            Typ = r.RowType == RowType.Material ? "Materiál" : "Práca",
            Položka = (r.RowType == RowType.Material ? r.Material?.Name : r.Operation?.Name) ?? "",
            Quantity = r.Quantity,
            DimensionA = r.DimensionA,
            DimensionB = r.DimensionB,
        }).ToList();

        _rowsSource.DataSource = rows;
        gridRows.DataSource = _rowsSource;

        if (gridRows.Columns.Count > 0)
        {
            gridRows.Columns["Id"].Visible = false;
            gridRows.Columns["Typ"].HeaderText = "Typ";
            gridRows.Columns["Položka"].HeaderText = "Položka";
            gridRows.Columns["Quantity"].HeaderText = "Množstvo";
            gridRows.Columns["DimensionA"].HeaderText = "Rozmer A";
            gridRows.Columns["DimensionB"].HeaderText = "Rozmer B";
        }
    }

    private void RefreshSummary()
    {
        var summary = PriceCalculator.Calculate(_product);
        lblMaterialTotal.Text = $"Materiál:  {summary.MaterialTotal:N4}";
        lblOperationTotal.Text = $"Práca:     {summary.OperationTotal:N4}";
        lblUvn.Text = $"UVN:       {summary.Uvn:N4}";
        lblBasePrice.Text = $"Cena:      {summary.BasePrice:N4}";
        lblPrice1.Text = $"Cena +8%:  {summary.Price1:N4}";
        lblPrice2.Text = $"Cena +16%: {summary.Price2:N4}";
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Výrobok musí mať názov.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _product.Name = txtName.Text.Trim();
        _product.Mode = (CalculationMode)cmbMode.SelectedItem!;
        _db.SaveChanges();
        RefreshSummary();
        Text = $"Výrobok — {_product.Name}";
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

    private void btnDeleteRow_Click(object sender, EventArgs e)
    {
        if (_rowsSource.Current is not RowEditModel model) return;
        var row = _product.CompositionRows.FirstOrDefault(r => r.Id == model.Id);
        if (row == null) return;

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
