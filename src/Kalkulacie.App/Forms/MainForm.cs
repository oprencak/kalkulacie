namespace Kalkulacie.App.Forms;

public partial class MainForm : AppForm
{
    public MainForm()
    {
        InitializeComponent();
        UpdateDbInfo();
    }

    private void UpdateDbInfo()
    {
        Text = $"Kalkulácie — {AppDb.DbName}";
        lblVersion.Text = $"Verzia {AppInfo.Version}";
        lblDb.Text = $"Databáza: {AppDb.DbName}";
        toolTip.SetToolTip(lblDb, AppDb.DbPath);

        if (AppDb.BackupError is not null)
        {
            lblDb.Text += " — záloha zlyhala!";
            toolTip.SetToolTip(lblDb, $"{AppDb.DbPath}\nAutomatická záloha zlyhala: {AppDb.BackupError}");
        }
    }

    private void btnChangeDb_Click(object sender, EventArgs e)
    {
        using var picker = new DatabasePickerForm();
        if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedPath is null) return;
        if (string.Equals(picker.SelectedPath, AppDb.DbPath, StringComparison.OrdinalIgnoreCase)) return;

        var previous = AppDb.DbPath!;
        try
        {
            AppDb.Open(picker.SelectedPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Databázu sa nepodarilo otvoriť:\n{ex.Message}", "Chyba",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            AppDb.Open(previous);
        }
        UpdateDbInfo();
    }

    private void btnKalkulacie_Click(object sender, EventArgs e)
    {
        using var form = new PriceListForm();
        form.ShowDialog(this);
    }

    private void btnProducts_Click(object sender, EventArgs e)
    {
        using var form = new ProductsForm();
        form.ShowDialog(this);
    }

    private void btnMaterials_Click(object sender, EventArgs e)
    {
        using var form = new MaterialsForm();
        form.ShowDialog(this);
    }

    private void btnSettings_Click(object sender, EventArgs e)
    {
        using var form = new PricingSettingsForm();
        form.ShowDialog(this);
    }

    private void btnOperations_Click(object sender, EventArgs e)
    {
        using var form = new OperationsForm();
        form.ShowDialog(this);
    }
}
