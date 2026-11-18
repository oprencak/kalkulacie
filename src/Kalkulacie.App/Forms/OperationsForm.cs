using System.ComponentModel;
using Kalkulacie.Core;
using Kalkulacie.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kalkulacie.App.Forms;

public partial class OperationsForm : AppForm
{
    private readonly KalkulacieDbContext _db;
    private readonly BindingSource _bindingSource = new();

    public OperationsForm()
    {
        InitializeComponent();
        _db = AppDb.CreateContext();
        Load += OnLoad;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        _db.Operations.Load();
        RefreshView();
        grid.DataSource = _bindingSource;

        grid.Columns["Id"]!.Visible = false;
        grid.Columns["CompositionRows"]!.Visible = false;
        grid.Columns["Name"]!.HeaderText = "Názov";
        grid.Columns["Rate"]!.HeaderText = "Sadzba";
    }

    /// <summary>
    /// Naplní grid réžiami vyhovujúcimi hľadaniu, zoradenými podľa názvu.
    /// Neuložené nové réžie sa zobrazujú vždy (na konci), aby sa nestratili z dohľadu.
    /// </summary>
    private void RefreshView()
    {
        grid.EndEdit();
        var term = txtSearch.Text.Trim();
        var current = _bindingSource.Current as Operation;

        var items = _db.Operations.Local
            .Where(o => o.Id == 0 || TextSearch.Matches(o.Name, term))
            .OrderBy(o => o.Id == 0)
            .ThenBy(o => o.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        _bindingSource.DataSource = new BindingList<Operation>(items);

        var index = current is null ? -1 : items.IndexOf(current);
        if (index >= 0) _bindingSource.Position = index;
    }

    private void txtSearch_TextChanged(object sender, EventArgs e) => RefreshView();

    private void btnAdd_Click(object sender, EventArgs e)
    {
        txtSearch.Clear();

        var operation = new Operation();
        _db.Operations.Add(operation);
        _bindingSource.Add(operation);
        _bindingSource.Position = _bindingSource.Count - 1;

        grid.CurrentCell = grid.Rows[^1].Cells["Name"];
        grid.BeginEdit(true);
    }

    private void btnDelete_Click(object sender, EventArgs e)
    {
        if (_bindingSource.Current is not Operation op) return;

        if (op.Id != 0)
        {
            int usedIn = _db.CompositionRows
                .Where(r => r.OperationId == op.Id)
                .Select(r => r.ProductId)
                .Distinct()
                .Count();
            if (usedIn > 0)
            {
                MessageBox.Show(
                    $"Réžia '{op.Name}' sa používa vo výrobkoch (počet: {usedIn}) a nedá sa vymazať.\n\n" +
                    "Zoznam výrobkov zobrazí tlačidlo „Kde sa používa“.",
                    "Réžia sa používa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        if (MessageBox.Show($"Vymazať réžiu '{op.Name}'?", "Potvrdiť vymazanie",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _db.Operations.Remove(op);
        _bindingSource.RemoveCurrent();
        Save();
    }

    private void btnUsage_Click(object sender, EventArgs e)
    {
        grid.EndEdit();
        if (_bindingSource.Current is not Operation op) return;

        // Rovnaký kontext ako grid — výpočet tak zohľadní aj ešte neuložené zmeny sadzieb.
        var products = op.Id == 0
            ? []
            : _db.Products
                .Include(p => p.CompositionRows).ThenInclude(r => r.Material)
                .Include(p => p.CompositionRows).ThenInclude(r => r.Operation)
                .Where(p => p.CompositionRows.Any(r => r.OperationId == op.Id))
                .ToList();

        UsageForm.ShowFor(this, op.Name, products, r => r.OperationId == op.Id, _db.GetPricingSettings());
    }

    private void btnSave_Click(object sender, EventArgs e) => Save();

    private void Save()
    {
        grid.EndEdit();
        var invalid = _db.Operations.Local.Where(o => string.IsNullOrWhiteSpace(o.Name)).ToList();
        if (invalid.Count > 0)
        {
            MessageBox.Show("Každá réžia musí mať názov.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
