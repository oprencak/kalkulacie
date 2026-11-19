using Kalkulacie.App.Update;

namespace Kalkulacie.App.Forms;

public partial class MainForm : AppForm
{
    public MainForm()
    {
        InitializeComponent();
        Theme.Primary(btnUpdate);
        UpdateDbInfo();
        Shown += async (_, _) => await CheckForUpdateAsync();
    }

    private ReleaseInfo? _release;

    /// <summary>
    /// Na pozadí zistí, či je na GitHube novšia verzia. Štart nezdržuje; bez internetu alebo pri chybe
    /// sa nestane nič.
    /// </summary>
    private async Task CheckForUpdateAsync()
    {
        var release = await Task.Run(() => Updater.CheckAsync());
        if (release is null || IsDisposed) return;

        _release = release;
        lblUpdate.Text = $"Nová verzia {release.Version.ToString(3)} je dostupná";
        if (!pnlUpdate.Visible)
        {
            pnlUpdate.Visible = true;
            ClientSize = new Size(ClientSize.Width, pnlUpdate.Bottom + pnlUpdate.Left);
        }
    }

    private void btnUpdate_Click(object sender, EventArgs e)
    {
        if (_release is null) return;
        using var form = new UpdateForm(_release);
        if (form.ShowDialog(this) == DialogResult.OK)
            Application.Exit(); // nová verzia sa už spúšťa a čaká na skončenie tejto
    }

    private void UpdateDbInfo()
    {
        Text = $"Kalkulácie — {AppDb.DbName}";
        lblDb.Text = $"Databáza: {AppDb.DbName}";
        toolTip.SetToolTip(lblDb, AppDb.DbPath);

        if (AppDb.BackupError is not null)
        {
            lblDb.Text += " — záloha zlyhala!";
            toolTip.SetToolTip(lblDb, $"{AppDb.DbPath}\nAutomatická záloha zlyhala: {AppDb.BackupError}");
        }
    }

    private readonly Bitmap _logo = new Icon(AppInfo.Icon, 48, 48).ToBitmap();
    private readonly Font _titleFont = new("Segoe UI Semibold", 15F);
    private readonly Font _versionFont = new("Segoe UI", 9F);

    private void PaintHeader(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        int height = pnlHeader.Height;
        int logo = height * 48 / 76, margin = (height - logo) / 2;
        g.DrawImage(_logo, btnKalkulacie.Left, margin, logo, logo);

        int textX = btnKalkulacie.Left + logo + margin;
        int y = (height - _titleFont.Height - _versionFont.Height) / 2 - 1;
        using var light = new SolidBrush(Color.FromArgb(200, 232, 226));
        g.DrawString("Kalkulácie", _titleFont, Brushes.White, textX - 3, y);
        g.DrawString($"Verzia {AppInfo.Version}", _versionFont, light, textX, y + _titleFont.Height);
    }

    private void PaintUpdatePanel(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(Theme.WarningBorder);
        e.Graphics.DrawRectangle(pen, 0, 0, pnlUpdate.Width - 1, pnlUpdate.Height - 1);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _logo.Dispose();
        _titleFont.Dispose();
        _versionFont.Dispose();
        base.OnFormClosed(e);
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

    private void btnEditions_Click(object sender, EventArgs e)
    {
        using var form = new CalculationEditionsForm();
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
