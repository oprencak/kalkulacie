using Kalkulacie.Core;
using Kalkulacie.Core.Calculations;
using Kalkulacie.Core.Entities;

namespace Kalkulacie.App.Forms;

public partial class OperationRowForm : AppForm
{
    private readonly KalkulacieDbContext _db;
    private List<Operation> _all = [];
    private PricingSettings _settings = new();

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
        _settings = _db.GetPricingSettings();
        _all = _db.Operations.AsEnumerable()
            .OrderBy(o => o.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        lstOperation.DisplayMember = "Name";
        ApplyFilter(existing?.OperationId);
        ActiveControl = txtSearch;
        lstOperation.SelectedIndexChanged += (_, _) => UpdateCost();
        numQty.ValueChanged += (_, _) => UpdateCost();

        if (existing != null)
        {
            Text = "Upraviť pracovný riadok";
            numQty.Value = (decimal)existing.Quantity;
        }

        UpdateCost();
    }

    /// <summary>Zobrazí položky vyhovujúce hľadaniu; ponechá (alebo nastaví) vybranú položku.</summary>
    private void ApplyFilter(int? selectId = null)
    {
        selectId ??= (lstOperation.SelectedItem as Operation)?.Id;
        var term = txtSearch.Text.Trim();
        var items = _all.Where(x => TextSearch.Matches(x.Name, term)).ToList();

        lstOperation.DataSource = items;
        var index = items.FindIndex(x => x.Id == selectId);
        lstOperation.SelectedIndex = index >= 0 ? index : (items.Count > 0 ? 0 : -1);
    }

    /// <summary>Ukáže, ako sa z počtu kusov počíta náklad: sadzba réžie delená počtom kusov.</summary>
    private void UpdateCost()
    {
        lblCost.Text = lstOperation.SelectedItem is Operation op
            ? $"Náklad na kus = sadzba / počet kusov\n{op.Rate:0.####} / {numQty.Value:0.####} = " +
              $"{PriceCalculator.OperationCost(op.Rate, numQty.Value, _settings):N4}"
            : "Náklad na kus = sadzba / počet kusov";
    }

    private void txtSearch_TextChanged(object sender, EventArgs e) => ApplyFilter();

    // Šípky hore/dole v hľadaní posúvajú výber v zozname
    private void txtSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode is not (Keys.Down or Keys.Up)) return;
        int index = lstOperation.SelectedIndex + (e.KeyCode == Keys.Down ? 1 : -1);
        if (index >= 0 && index < lstOperation.Items.Count) lstOperation.SelectedIndex = index;
        e.Handled = true;
    }

    private void btnOk_Click(object sender, EventArgs e)
    {
        if (lstOperation.SelectedItem is not Operation op)
        {
            MessageBox.Show("Vyberte réžiu / prácu.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (numQty.Value <= 0)
        {
            MessageBox.Show("Počet kusov musí byť väčší ako 0.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SelectedOperation = op;
        Quantity = numQty.Value;
        DialogResult = DialogResult.OK;
    }
}
