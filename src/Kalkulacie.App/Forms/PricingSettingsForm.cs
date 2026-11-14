using Kalkulacie.Core.Entities;

namespace Kalkulacie.App.Forms;

/// <summary>Úprava koeficientov cenových vzorcov aktívnej databázy. Prirážky sa zadávajú v percentách.</summary>
public class PricingSettingsForm : AppForm
{
    private readonly NumericUpDown _areaManual = Percent();
    private readonly NumericUpDown _areaAutomatic = Percent();
    private readonly NumericUpDown _piece = Percent();
    private readonly NumericUpDown _marginManual = Percent();
    private readonly NumericUpDown _marginAutomatic = Percent();
    private readonly CheckBox _chkRound = new() { Text = "Zaokrúhľovať riadky a ceny na", AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly NumericUpDown _roundDecimals = new() { Minimum = 0, Maximum = 6, Width = 50, Anchor = AnchorStyles.Left };

    private PricingSettings _original = new();

    public PricingSettingsForm()
    {
        Text = $"Koeficienty — {AppDb.DbName}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        var table = new TableLayoutPanel { AutoSize = true, ColumnCount = 3, Dock = DockStyle.Fill };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddHeader(table, "Materiál — prirážka na odpad");
        AddRow(table, "Plošný materiál, ručná výroba:", _areaManual);
        AddRow(table, "Plošný materiál, strojová výroba:", _areaAutomatic);
        AddRow(table, "Kusový materiál:", _piece);
        AddHeader(table, "Cena — prirážka k UVN");
        AddRow(table, "Ručná výroba:", _marginManual);
        AddRow(table, "Strojová výroba:", _marginAutomatic);
        AddHeader(table, "Zaokrúhľovanie");
        table.Controls.Add(_chkRound);
        table.Controls.Add(_roundDecimals);
        table.Controls.Add(UnitLabel("desatinné miesta"));

        var btnDefaults = new Button { Text = "Obnoviť pôvodné", AutoSize = true };
        var btnSave = new Button { Text = "Uložiť", AutoSize = true };
        var btnCancel = new Button { Text = "Zrušiť", AutoSize = true, DialogResult = DialogResult.Cancel };
        btnDefaults.Click += (_, _) => Show(new PricingSettings());
        btnSave.Click += (_, _) => Save();
        _chkRound.CheckedChanged += (_, _) => _roundDecimals.Enabled = _chkRound.Checked;

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 14, 0, 0),
        };
        buttons.Controls.AddRange([btnCancel, btnSave, btnDefaults]);
        table.Controls.Add(buttons);
        table.SetColumnSpan(buttons, 3);

        Controls.Add(table);
        AcceptButton = btnSave;
        CancelButton = btnCancel;

        Load += (_, _) =>
        {
            using var db = AppDb.CreateContext();
            _original = db.GetPricingSettings();
            Show(_original);
        };
    }

    private static NumericUpDown Percent() => new()
    {
        DecimalPlaces = 2,
        Minimum = -99.99m,
        Maximum = 999.99m,
        Width = 80,
        TextAlign = HorizontalAlignment.Right,
        Anchor = AnchorStyles.Left,
    };

    private static Label UnitLabel(string text) =>
        new() { Text = text, AutoSize = true, Anchor = AnchorStyles.Left };

    private static void AddHeader(TableLayoutPanel table, string text)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font(DefaultFont, FontStyle.Bold),
            Margin = new Padding(3, table.Controls.Count == 0 ? 0 : 12, 3, 4),
        };
        table.Controls.Add(label);
        table.SetColumnSpan(label, 3);
    }

    private static void AddRow(TableLayoutPanel table, string text, NumericUpDown input)
    {
        table.Controls.Add(new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(12, 3, 12, 3) });
        table.Controls.Add(input);
        table.Controls.Add(UnitLabel("%"));
    }

    private static decimal ToPercent(decimal coefficient) =>
        Math.Clamp((coefficient - 1) * 100, -99.99m, 999.99m);

    private static decimal ToCoefficient(NumericUpDown input) => 1 + input.Value / 100;

    private void Show(PricingSettings s)
    {
        _areaManual.Value = ToPercent(s.AreaMaterialManual);
        _areaAutomatic.Value = ToPercent(s.AreaMaterialAutomatic);
        _piece.Value = ToPercent(s.PieceMaterial);
        _marginManual.Value = ToPercent(s.MarginManual);
        _marginAutomatic.Value = ToPercent(s.MarginAutomatic);
        _chkRound.Checked = s.RoundingDecimals.HasValue;
        _roundDecimals.Value = Math.Clamp(s.RoundingDecimals ?? 3, 0, 6);
        _roundDecimals.Enabled = _chkRound.Checked;
    }

    private void Apply(PricingSettings s)
    {
        s.AreaMaterialManual = ToCoefficient(_areaManual);
        s.AreaMaterialAutomatic = ToCoefficient(_areaAutomatic);
        s.PieceMaterial = ToCoefficient(_piece);
        s.MarginManual = ToCoefficient(_marginManual);
        s.MarginAutomatic = ToCoefficient(_marginAutomatic);
        s.RoundingDecimals = _chkRound.Checked ? (int)_roundDecimals.Value : null;
    }

    private bool IsChanged()
    {
        var edited = new PricingSettings();
        Apply(edited);
        return edited.AreaMaterialManual != _original.AreaMaterialManual
            || edited.AreaMaterialAutomatic != _original.AreaMaterialAutomatic
            || edited.PieceMaterial != _original.PieceMaterial
            || edited.MarginManual != _original.MarginManual
            || edited.MarginAutomatic != _original.MarginAutomatic
            || edited.RoundingDecimals != _original.RoundingDecimals;
    }

    private void Save()
    {
        if (!IsChanged())
        {
            DialogResult = DialogResult.Cancel;
            return;
        }

        if (MessageBox.Show(this,
                $"Zmena sa okamžite prejaví v cenách všetkých výrobkov v databáze „{AppDb.DbName}“.\n\n" +
                "Pôvodné ceny sa neuchovávajú. Uložiť?",
                "Zmena koeficientov", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        using var db = AppDb.CreateContext();
        var settings = db.PricingSettings.FirstOrDefault();
        if (settings is null)
        {
            settings = new PricingSettings();
            db.PricingSettings.Add(settings);
        }
        Apply(settings);
        db.SaveChanges();
        DialogResult = DialogResult.OK;
    }
}
