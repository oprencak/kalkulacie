using Kalkulacie.Core;
using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kalkulacie.App.Forms;

public partial class ProductsForm : Form
{
    private readonly KalkulacieDbContext _db;
    private BindingSource _bindingSource = new();

    public ProductsForm(CalculationMode? initialFilter = null)
    {
        InitializeComponent();
        _db = AppDb.CreateContext();
        Load += (_, _) =>
        {
            if (initialFilter == CalculationMode.Manual) rdManual.Checked = true;
            else if (initialFilter == CalculationMode.Automatic) rdAutomatic.Checked = true;
            OnLoad(null, EventArgs.Empty);
        };
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        RefreshList();
        if (grid.DataSource == null) grid.DataSource = _bindingSource;

        grid.Columns["Id"].Visible = false;
        grid.Columns["CompositionRows"].Visible = false;
        grid.Columns["Name"].HeaderText = "Názov";
        grid.Columns["Mode"].HeaderText = "Typ";
    }

    private void RefreshList()
    {
        _db.ChangeTracker.Clear();
        _db.Products.Load();

        var all = _db.Products.Local.ToList();
        var filtered = rdManual.Checked ? all.Where(p => p.Mode == CalculationMode.Manual).ToList()
                     : rdAutomatic.Checked ? all.Where(p => p.Mode == CalculationMode.Automatic).ToList()
                     : all;

        _bindingSource.DataSource = filtered;
    }

    private void rdFilter_CheckedChanged(object sender, EventArgs e)
    {
        if (((RadioButton)sender).Checked)
            RefreshList();
    }

    private void btnAdd_Click(object sender, EventArgs e)
    {
        var mode = rdAutomatic.Checked ? CalculationMode.Automatic : CalculationMode.Manual;
        var product = new Product { Name = "Nový výrobok", Mode = mode };
        _db.Products.Add(product);
        _db.SaveChanges();
        var id = product.Id;
        RefreshList();
        OpenDetail(id);
    }

    private void btnDetail_Click(object sender, EventArgs e)
    {
        if (_bindingSource.Current is not Product product) return;
        OpenDetail(product.Id);
    }

    private void btnDelete_Click(object sender, EventArgs e)
    {
        if (_bindingSource.Current is not Product product) return;
        if (MessageBox.Show($"Vymazať výrobok '{product.Name}'?", "Potvrdiť vymazanie",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _db.Products.Remove(product);
        _db.SaveChanges();
        RefreshList();
    }

    private void btnClone_Click(object sender, EventArgs e)
    {
        if (_bindingSource.Current is not Product source) return;

        using var dlg = new CloneProductForm(source.Name);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var newName = dlg.NewName.Trim();
        if (string.IsNullOrWhiteSpace(newName)) return;

        _db.ChangeTracker.Clear();
        var original = _db.Products
            .Include(p => p.CompositionRows)
            .First(p => p.Id == source.Id);

        var clone = new Product { Name = newName, Mode = original.Mode };
        foreach (var row in original.CompositionRows)
        {
            clone.CompositionRows.Add(new Core.Entities.CompositionRow
            {
                RowType    = row.RowType,
                MaterialId = row.MaterialId,
                OperationId = row.OperationId,
                DimensionA = row.DimensionA,
                DimensionB = row.DimensionB,
                Quantity   = row.Quantity
            });
        }

        _db.Products.Add(clone);
        _db.SaveChanges();
        RefreshList();
        OpenDetail(clone.Id);
    }

    private void grid_DoubleClick(object sender, EventArgs e)
    {
        if (_bindingSource.Current is Product product)
            OpenDetail(product.Id);
    }

    private void OpenDetail(int productId)
    {
        using var form = new ProductDetailForm(productId);
        form.ShowDialog(this);
        RefreshList();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _db.Dispose();
        base.OnFormClosed(e);
    }
}
