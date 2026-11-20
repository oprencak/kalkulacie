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

    private readonly ToolTip _help = new() { AutoPopDelay = 20000, InitialDelay = 400, ReshowDelay = 100 };
    private readonly ToolTip _popup = new();
    private readonly Dictionary<Control, string> _helpTexts = [];
    private Control? _popupOwner;
    private Control? _hoverMuted;
    private Control? _justClosed;
    private IMessageFilter? _popupCloser;

    private PricingSettings _original = new();

    public PricingSettingsForm()
    {
        Text = $"Koeficienty — {AppDb.DbName}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        HelpButton = true; // „?“ v titulku: po kliknutí naň a na položku sa zobrazí jej nápoveda (ako F1)
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        var table = new TableLayoutPanel { AutoSize = true, ColumnCount = 4, Dock = DockStyle.Fill };
        for (int i = 0; i < table.ColumnCount; i++)
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddNote(table,
            "Koeficienty určujú, o koľko percent sa pri výpočte navýši náklad na materiál a cena výrobku. " +
            "Platia pre všetky výrobky v tejto databáze. Podrobnosti k položke zobrazíte ukázaním na ikonu " +
            "informácie alebo klávesom F1.");

        AddHeader(table, "Materiál — prirážka na odpad");
        AddNote(table,
            "Pripočíta sa k nákladu každého riadku materiálu v zložení výrobku — pokrýva odpad a straty " +
            "pri spracovaní. Na réžiu a prácu sa nevzťahuje.");
        AddRow(table, "Plošný materiál, ručná výroba:", _areaManual,
            "Kde sa použije: riadok materiálu zadaný ako Plocha (rozmery A a B)\n" +
            "vo výrobku typu ručná výroba.\n\n" +
            "Náklad riadku = A × B × cena materiálu × množstvo + prirážka\n" +
            "Príklad: pri 15 % sa z nákladu 10,00 stane 11,50.");
        AddRow(table, "Plošný materiál, strojová výroba:", _areaAutomatic,
            "Kde sa použije: riadok materiálu zadaný ako Plocha (rozmery A a B)\n" +
            "vo výrobku typu strojová výroba.\n\n" +
            "Náklad riadku = A × B × cena materiálu × množstvo + prirážka\n" +
            "Príklad: pri 5 % sa z nákladu 10,00 stane 10,50.");
        AddRow(table, "Kusový materiál:", _piece,
            "Kde sa použije: riadok materiálu zadaný ako Kusy (bez rozmerov),\n" +
            "pri ručnej aj strojovej výrobe.\n\n" +
            "Náklad riadku = cena materiálu × množstvo + prirážka\n" +
            "Príklad: pri 3 % sa z nákladu 10,00 stane 10,30.");

        AddHeader(table, "Cena — prirážka k UVN");
        AddNote(table,
            "UVN (úplné vlastné náklady) je súčet nákladov na materiál a prácu. Pripočítaním prirážky " +
            "vznikne cena výrobku, ktorú vidíte v detaile výrobku a v cenníku.");
        AddRow(table, "Ručná výroba:", _marginManual,
            "Kde sa použije: cena každého výrobku typu ručná výroba.\n\n" +
            "Cena = UVN + prirážka\n" +
            "Príklad: pri 15 % je z UVN 100,00 cena 115,00.");
        AddRow(table, "Strojová výroba:", _marginAutomatic,
            "Kde sa použije: cena každého výrobku typu strojová výroba.\n\n" +
            "Cena = UVN + prirážka\n" +
            "Príklad: pri 8 % je z UVN 100,00 cena 108,00.");

        AddHeader(table, "Zaokrúhľovanie");
        AddNote(table,
            "Zaokrúhľuje sa náklad každého riadku zloženia, UVN aj výsledná cena. " +
            "Bez zaokrúhľovania sa počíta s plnou presnosťou.");
        const string roundingHelp =
            "Kde sa použije: náklad každého riadku zloženia, UVN a cena výrobku.\n\n" +
            "Polovice sa zaokrúhľujú od nuly.\n" +
            "Príklad: pri 3 miestach sa z 1,2345 stane 1,235.\n" +
            "Po vypnutí sa nezaokrúhľuje nič.";
        table.Controls.Add(_chkRound);
        table.Controls.Add(_roundDecimals);
        table.Controls.Add(UnitLabel("desatinné miesta"));
        table.Controls.Add(InfoIcon("Zaokrúhľovanie", roundingHelp));
        SetHelp(roundingHelp, _chkRound, _roundDecimals);

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
        table.SetColumnSpan(buttons, table.ColumnCount);

        Controls.Add(table);
        AcceptButton = btnSave;
        CancelButton = btnCancel;

        // Bublina pri ukázaní myšou sa neukazuje cez bublinu otvorenú kliknutím / F1
        _help.Popup += (_, e) => e.Cancel = _popupOwner is not null || e.AssociatedControl == _hoverMuted;

        Load += (_, _) =>
        {
            // Vysvetľujúce texty sa zalomia na šírku, ktorú určili riadky s položkami (závisí od DPI)
            int width = table.GetColumnWidths().Sum();
            foreach (var note in table.Controls.OfType<Label>().Where(l => l.MaximumSize.Width > 0))
                note.MaximumSize = new Size(width - note.Margin.Horizontal, 0);

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
        table.SetColumnSpan(label, table.ColumnCount);
    }

    /// <summary>Vysvetľujúci text pod nadpisom skupiny — tlmený, zalomený (šírka sa doladí pri načítaní okna).</summary>
    private static void AddNote(TableLayoutPanel table, string text)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(400, 0),
            ForeColor = Theme.TextMuted,
            Margin = new Padding(3, 0, 3, 8),
        };
        table.Controls.Add(label);
        table.SetColumnSpan(label, table.ColumnCount);
    }

    private void AddRow(TableLayoutPanel table, string text, NumericUpDown input, string help)
    {
        var label = new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(12, 3, 12, 3) };
        table.Controls.Add(label);
        table.Controls.Add(input);
        table.Controls.Add(UnitLabel("%"));
        table.Controls.Add(InfoIcon(text.TrimEnd(':'), help));
        SetHelp(help, label, input);
    }

    /// <summary>Ikona informácie: nápoveda sa ukáže pri ukázaní myšou aj po kliknutí.</summary>
    private Label InfoIcon(string name, string help)
    {
        var icon = new Label
        {
            Text = "\uE946",
            Font = new Font("Segoe MDL2 Assets", 10f),
            ForeColor = Theme.Accent,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Cursor = Cursors.Help,
            Margin = new Padding(6, 3, 0, 3),
            AccessibleRole = AccessibleRole.HelpBalloon,
            AccessibleName = $"Nápoveda: {name}",
            AccessibleDescription = help,
        };
        // Kliknutie bublinu prepína: stlačenie myši otvorenú bublinu zavrie a to isté kliknutie ju už neotvorí
        bool closing = false;
        icon.MouseDown += (_, _) => closing = _justClosed == icon;
        icon.Click += (_, _) =>
        {
            if (!closing)
                ShowHelp(icon, help);
            closing = false;
        };
        SetHelp(help, icon);
        return icon;
    }

    /// <summary>Nápoveda k ovládacím prvkom: bublina pri ukázaní myšou, po F1 a v režime „?“ z titulku okna.</summary>
    private void SetHelp(string help, params Control[] controls)
    {
        foreach (var control in controls)
        {
            _helpTexts[control] = help;
            _help.SetToolTip(control, help);
            control.MouseLeave += (_, _) =>
            {
                if (_hoverMuted == control)
                    _hoverMuted = null;
            };
            control.HelpRequested += (_, e) =>
            {
                ShowHelp(control, help);
                e.Handled = true;
            };
        }
    }

    /// <summary>
    /// Bublina na vyžiadanie (kliknutie, F1, „?“). Má vlastný <see cref="ToolTip"/>, aby sa nemiešala s bublinou
    /// pri ukázaní myšou, a zavrie sa pri najbližšom kliknutí alebo stlačení klávesu kdekoľvek v aplikácii.
    /// </summary>
    private void ShowHelp(Control control, string help)
    {
        HidePopup();
        _help.Hide(control);
        _popupOwner = control;
        // Kým myš z prvku neodíde, bublina pri ukázaní myšou sa nad ním znova neukáže (ani po zavretí tejto)
        if (control.ClientRectangle.Contains(control.PointToClient(Cursor.Position)))
            _hoverMuted = control;
        _popup.Show(help, control, 0, control.Height + 2);
        // Až po spracovaní práve prebiehajúceho kliknutia / klávesu, inak by bublinu hneď zavrelo
        BeginInvoke(() =>
        {
            if (_popupOwner == control && _popupCloser is null)
                Application.AddMessageFilter(_popupCloser = new PopupCloser(HidePopup));
        });
    }

    private void HidePopup()
    {
        if (_popupCloser is not null)
        {
            Application.RemoveMessageFilter(_popupCloser);
            _popupCloser = null;
        }
        if (_popupOwner is { IsDisposed: false } owner)
        {
            _popup.Hide(owner);
            _justClosed = owner;
            if (IsHandleCreated)
                BeginInvoke(() => _justClosed = null); // platí len pre práve spracúvanú správu
        }
        _popupOwner = null;
    }

    /// <summary>F1 v poli s koeficientom ukáže jeho nápovedu.</summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F1 && ActiveControl is { } active && _helpTexts.TryGetValue(active, out var help))
        {
            ShowHelp(active, help);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        HidePopup();
        base.OnDeactivate(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            HidePopup();
            _help.Dispose();
            _popup.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>Zavrie bublinu pri stlačení tlačidla myši alebo klávesu; samotnú správu nezadrží.</summary>
    private sealed class PopupCloser(Action close) : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {
            const int WM_KEYDOWN = 0x100, WM_SYSKEYDOWN = 0x104, WM_MOUSEWHEEL = 0x20A;
            const int WM_LBUTTONDOWN = 0x201, WM_RBUTTONDOWN = 0x204, WM_MBUTTONDOWN = 0x207;
            const int WM_NCLBUTTONDOWN = 0xA1, WM_NCRBUTTONDOWN = 0xA4;
            if (m.Msg is WM_KEYDOWN or WM_SYSKEYDOWN or WM_MOUSEWHEEL or WM_LBUTTONDOWN or WM_RBUTTONDOWN
                or WM_MBUTTONDOWN or WM_NCLBUTTONDOWN or WM_NCRBUTTONDOWN)
                close();
            return false;
        }
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
