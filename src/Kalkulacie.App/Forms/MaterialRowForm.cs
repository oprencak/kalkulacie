using Kalkulacie.Core;
using Kalkulacie.Core.Entities;

namespace Kalkulacie.App.Forms;

public partial class MaterialRowForm : AppForm
{
    private readonly KalkulacieDbContext _db;
    private List<Material> _all = [];

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
        _all = _db.Materials.AsEnumerable()
            .OrderBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        lstMaterial.DisplayMember = "Name";
        ApplyFilter(existing?.MaterialId);
        ActiveControl = txtSearch;

        if (existing != null)
        {
            Text = "Upraviť materiálový riadok";
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

    /// <summary>Zobrazí položky vyhovujúce hľadaniu; ponechá (alebo nastaví) vybranú položku.</summary>
    private void ApplyFilter(int? selectId = null)
    {
        selectId ??= (lstMaterial.SelectedItem as Material)?.Id;
        var term = txtSearch.Text.Trim();
        var items = _all.Where(x => TextSearch.Matches(x.Name, term)).ToList();

        lstMaterial.DataSource = items;
        var index = items.FindIndex(x => x.Id == selectId);
        lstMaterial.SelectedIndex = index >= 0 ? index : (items.Count > 0 ? 0 : -1);
    }

    private void txtSearch_TextChanged(object sender, EventArgs e) => ApplyFilter();

    // Šípky hore/dole v hľadaní posúvajú výber v zozname
    private void txtSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode is not (Keys.Down or Keys.Up)) return;
        int index = lstMaterial.SelectedIndex + (e.KeyCode == Keys.Down ? 1 : -1);
        if (index >= 0 && index < lstMaterial.Items.Count) lstMaterial.SelectedIndex = index;
        e.Handled = true;
    }

    private void btnOk_Click(object sender, EventArgs e)
    {
        if (lstMaterial.SelectedItem is not Material mat)
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
