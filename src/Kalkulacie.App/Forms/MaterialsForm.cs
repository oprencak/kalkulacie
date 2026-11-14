using System.ComponentModel;
using Kalkulacie.Core;
using Kalkulacie.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kalkulacie.App.Forms;

public partial class MaterialsForm : AppForm
{
    private readonly KalkulacieDbContext _db;
    private readonly BindingSource _bindingSource = new();

    public MaterialsForm()
    {
        InitializeComponent();
        _db = AppDb.CreateContext();
        Load += OnLoad;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        _db.Materials.Load();
        RefreshView();
        grid.DataSource = _bindingSource;

        grid.Columns["Id"].Visible = false;
        grid.Columns["CompositionRows"].Visible = false;
        grid.Columns["Name"].HeaderText = "Názov";
        grid.Columns["Unit"].HeaderText = "MJ";
        grid.Columns["Price"].HeaderText = "Cena";
    }

    /// <summary>
    /// Naplní grid materiálmi vyhovujúcimi hľadaniu, zoradenými podľa názvu.
    /// Neuložené nové materiály sa zobrazujú vždy (na konci), aby sa nestratili z dohľadu.
    /// </summary>
    private void RefreshView()
    {
        grid.EndEdit();
        var term = txtSearch.Text.Trim();
        var current = _bindingSource.Current as Material;

        var items = _db.Materials.Local
            .Where(m => m.Id == 0 || TextSearch.Matches(m.Name, term))
            .OrderBy(m => m.Id == 0)
            .ThenBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        _bindingSource.DataSource = new BindingList<Material>(items);

        var index = current is null ? -1 : items.IndexOf(current);
        if (index >= 0) _bindingSource.Position = index;
    }

    private void txtSearch_TextChanged(object sender, EventArgs e) => RefreshView();

    private void btnAdd_Click(object sender, EventArgs e)
    {
        txtSearch.Clear();

        var material = new Material();
        _db.Materials.Add(material);
        _bindingSource.Add(material);
        _bindingSource.Position = _bindingSource.Count - 1;

        grid.CurrentCell = grid.Rows[^1].Cells["Name"];
        grid.BeginEdit(true);
    }

    private void btnDelete_Click(object sender, EventArgs e)
    {
        if (_bindingSource.Current is not Material mat) return;

        if (mat.Id != 0)
        {
            int usedIn = _db.CompositionRows
                .Where(r => r.MaterialId == mat.Id)
                .Select(r => r.ProductId)
                .Distinct()
                .Count();
            if (usedIn > 0)
            {
                MessageBox.Show(
                    $"Materiál '{mat.Name}' sa používa vo výrobkoch (počet: {usedIn}) a nedá sa vymazať.\n\n" +
                    "Zoznam výrobkov zobrazí tlačidlo „Kde sa používa“.",
                    "Materiál sa používa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        if (MessageBox.Show($"Vymazať materiál '{mat.Name}'?", "Potvrdiť vymazanie",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _db.Materials.Remove(mat);
        _bindingSource.RemoveCurrent();
        Save();
    }

    private void btnUsage_Click(object sender, EventArgs e)
    {
        grid.EndEdit();
        if (_bindingSource.Current is not Material mat) return;

        // Rovnaký kontext ako grid — výpočet tak zohľadní aj ešte neuložené zmeny cien.
        var products = mat.Id == 0
            ? []
            : _db.Products
                .Include(p => p.CompositionRows).ThenInclude(r => r.Material)
                .Include(p => p.CompositionRows).ThenInclude(r => r.Operation)
                .Where(p => p.CompositionRows.Any(r => r.MaterialId == mat.Id))
                .ToList();

        UsageForm.ShowFor(this, mat.Name, products, r => r.MaterialId == mat.Id, _db.GetPricingSettings());
    }

    private void btnSave_Click(object sender, EventArgs e) => Save();

    private void Save()
    {
        grid.EndEdit();
        var invalid = _db.Materials.Local.Where(m => string.IsNullOrWhiteSpace(m.Name)).ToList();
        if (invalid.Count > 0)
        {
            MessageBox.Show("Každý materiál musí mať názov.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _db.SaveChanges();
        RefreshView();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _db.Dispose();
        base.OnFormClosed(e);
    }
}
