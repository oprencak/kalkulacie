using Kalkulacie.Core;
using Kalkulacie.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kalkulacie.App.Forms;

public partial class OperationsForm : Form
{
    private readonly KalkulacieDbContext _db;
    private BindingSource _bindingSource = new();

    public OperationsForm()
    {
        InitializeComponent();
        _db = AppDb.CreateContext();
        Load += OnLoad;
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        _db.Operations.Load();
        _bindingSource.DataSource = _db.Operations.Local.ToBindingList();
        grid.DataSource = _bindingSource;

        grid.Columns["Id"].Visible = false;
        grid.Columns["CompositionRows"].Visible = false;
        grid.Columns["Name"].HeaderText = "Názov";
        grid.Columns["Rate"].HeaderText = "Sadzba";
    }

    private void btnAdd_Click(object sender, EventArgs e)
    {
        _bindingSource.AddNew();
        grid.CurrentCell = grid.Rows[^1].Cells["Name"];
        grid.BeginEdit(true);
    }

    private void btnDelete_Click(object sender, EventArgs e)
    {
        if (_bindingSource.Current is not Operation op) return;
        if (MessageBox.Show($"Vymazať réžiu '{op.Name}'?", "Potvrdiť vymazanie",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _bindingSource.RemoveCurrent();
        Save();
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
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _db.Dispose();
        base.OnFormClosed(e);
    }
}
