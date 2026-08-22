using Kalkulacie.Core;
using Kalkulacie.Core.Entities;

namespace Kalkulacie.App.Forms;

public partial class OperationRowForm : Form
{
    private readonly KalkulacieDbContext _db;

    public Operation? SelectedOperation { get; private set; }
    public decimal Quantity { get; private set; }

    public OperationRowForm(KalkulacieDbContext db, CompositionRow? existing = null)
    {
        InitializeComponent();
        _db = db;
        Load += (_, _) => OnLoad(existing);
    }

    private void OnLoad(CompositionRow? existing)
    {
        var operations = _db.Operations.OrderBy(o => o.Name).ToList();
        cmbOperation.DataSource = operations;
        cmbOperation.DisplayMember = "Name";
        cmbOperation.ValueMember = "Id";

        if (existing != null)
        {
            Text = "Upraviť pracovný riadok";
            cmbOperation.SelectedValue = existing.OperationId;
            numQty.Value = (decimal)existing.Quantity;
        }
    }

    private void btnOk_Click(object sender, EventArgs e)
    {
        if (cmbOperation.SelectedItem is not Operation op)
        {
            MessageBox.Show("Vyberte réžiu / prácu.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (numQty.Value <= 0)
        {
            MessageBox.Show("Množstvo musí byť väčšie ako 0.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SelectedOperation = op;
        Quantity = numQty.Value;
        DialogResult = DialogResult.OK;
    }
}
