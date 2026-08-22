using Kalkulacie.Core;
using Kalkulacie.Core.Entities;

namespace Kalkulacie.App.Forms;

public partial class MaterialRowForm : Form
{
    private readonly KalkulacieDbContext _db;

    public Material? SelectedMaterial { get; private set; }
    public bool IsArea { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal? DimensionA { get; private set; }
    public decimal? DimensionB { get; private set; }

    public MaterialRowForm(KalkulacieDbContext db, CompositionRow? existing = null)
    {
        InitializeComponent();
        _db = db;
        Load += (_, _) => OnLoad(existing);
    }

    private void OnLoad(CompositionRow? existing)
    {
        var materials = _db.Materials.OrderBy(m => m.Name).ToList();
        cmbMaterial.DataSource = materials;
        cmbMaterial.DisplayMember = "Name";
        cmbMaterial.ValueMember = "Id";

        if (existing != null)
        {
            Text = "Upraviť materiálový riadok";
            cmbMaterial.SelectedValue = existing.MaterialId;
            bool area = existing.DimensionA.HasValue || existing.DimensionB.HasValue;
            rdArea.Checked = area;
            rdPiece.Checked = !area;
            numQty.Value = (decimal)existing.Quantity;
            if (area)
            {
                numA.Value = (decimal)(existing.DimensionA ?? 0);
                numB.Value = (decimal)(existing.DimensionB ?? 0);
            }
        }
        else
        {
            rdPiece.Checked = true;
        }

        UpdateDimVisibility();
    }

    private void UpdateDimVisibility()
    {
        bool area = rdArea.Checked;
        lblA.Visible = area;
        numA.Visible = area;
        lblB.Visible = area;
        numB.Visible = area;
    }

    private void rdArea_CheckedChanged(object sender, EventArgs e) => UpdateDimVisibility();

    private void btnOk_Click(object sender, EventArgs e)
    {
        if (cmbMaterial.SelectedItem is not Material mat)
        {
            MessageBox.Show("Vyberte materiál.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (numQty.Value <= 0)
        {
            MessageBox.Show("Množstvo musí byť väčšie ako 0.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (rdArea.Checked && (numA.Value <= 0 || numB.Value <= 0))
        {
            MessageBox.Show("Rozmery musia byť väčšie ako 0.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SelectedMaterial = mat;
        IsArea = rdArea.Checked;
        Quantity = numQty.Value;
        DimensionA = IsArea ? numA.Value : null;
        DimensionB = IsArea ? numB.Value : null;
        DialogResult = DialogResult.OK;
    }
}
