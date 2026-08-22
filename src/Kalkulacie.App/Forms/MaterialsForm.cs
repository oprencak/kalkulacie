using Kalkulacie.Core;
using Kalkulacie.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kalkulacie.App.Forms;

public partial class MaterialsForm : Form
{
    private readonly KalkulacieDbContext _db;
    private BindingSource _bindingSource = new();

    public MaterialsForm()
    {
        InitializeComponent();
        _db = AppDb.CreateContext();
        Load += OnLoad;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        _db.Materials.Load();
        _bindingSource.DataSource = _db.Materials.Local.ToBindingList();
        grid.DataSource = _bindingSource;

        grid.Columns["Id"].Visible = false;
        grid.Columns["CompositionRows"].Visible = false;
        grid.Columns["Name"].HeaderText = "Názov";
        grid.Columns["Unit"].HeaderText = "MJ";
        grid.Columns["Price"].HeaderText = "Cena";
    }

    private void btnAdd_Click(object sender, EventArgs e)
    {
        _bindingSource.AddNew();
        grid.CurrentCell = grid.Rows[^1].Cells["Name"];
        grid.BeginEdit(true);
    }

    private void btnDelete_Click(object sender, EventArgs e)
    {
        if (_bindingSource.Current is not Material mat) return;
        if (MessageBox.Show($"Vymazať materiál '{mat.Name}'?", "Potvrdiť vymazanie",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _bindingSource.RemoveCurrent();
        Save();
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
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _db.Dispose();
        base.OnFormClosed(e);
    }
}
