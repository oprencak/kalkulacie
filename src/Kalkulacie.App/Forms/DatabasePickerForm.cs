using System.Diagnostics;

namespace Kalkulacie.App.Forms;

/// <summary>Výber, vytvorenie, obnova a mazanie databáz v priečinku <see cref="AppDb.DataDir"/>.</summary>
public class DatabasePickerForm : AppForm
{
    private readonly ListBox lstDatabases = new();
    private readonly Button btnOpen = new();
    private readonly Button btnCancel = new();
    private readonly Button btnFolder = new();
    private readonly Button btnRefresh = new();
    private readonly Button btnNew = new();
    private readonly Button btnDelete = new();

    /// <summary>Plná cesta k vybranej DB (po DialogResult.OK).</summary>
    public string? SelectedPath { get; private set; }

    private sealed record Item(string Path, string Text)
    {
        public override string ToString() => Text;
    }

    public DatabasePickerForm()
    {
        Text = "Výber databázy";
        Font = new Font("Segoe UI", 9.75F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(610, 330);

        var lblInfo = new Label
        {
            Text = $"Databázy v priečinku:\n{AppDb.DataDir}",
            Location = new Point(12, 10),
            Size = new Size(586, 40),
            AutoEllipsis = true,
        };

        lstDatabases.Location = new Point(12, 54);
        lstDatabases.Size = new Size(430, 220);
        lstDatabases.Font = new Font("Segoe UI", 11F);
        lstDatabases.IntegralHeight = false;
        lstDatabases.DoubleClick += (_, _) => Accept();
        lstDatabases.SelectedIndexChanged += (_, _) => UpdateButtons();
        lstDatabases.KeyDown += (_, e) => { if (e.KeyCode == Keys.Delete) DeleteSelected(); };

        btnNew.Text = "Nová…";
        btnNew.Location = new Point(454, 54);
        btnNew.Size = new Size(94, 32);
        btnNew.Click += (_, _) => CreateNew();

        btnDelete.Text = "Zmazať…";
        btnDelete.Location = new Point(454, 92);
        btnDelete.Size = new Size(94, 32);
        btnDelete.Click += (_, _) => DeleteSelected();

        var btnRestore = new Button
        {
            Text = "Obnoviť zo zálohy…",
            Location = new Point(454, 130),
            Size = new Size(144, 32),
        };
        btnRestore.Click += (_, _) => RestoreSelectedBackup();

        btnFolder.Text = "Otvoriť priečinok";
        btnFolder.Location = new Point(12, 286);
        btnFolder.Size = new Size(130, 32);
        btnFolder.Click += (_, _) =>
            Process.Start(new ProcessStartInfo { FileName = AppDb.DataDir, UseShellExecute = true });

        btnRefresh.Text = "Načítať zoznam";
        btnRefresh.Location = new Point(148, 286);
        btnRefresh.Size = new Size(115, 32);
        btnRefresh.Click += (_, _) => LoadItems();

        btnOpen.Text = "Otvoriť";
        btnOpen.Location = new Point(416, 286);
        btnOpen.Size = new Size(88, 32);
        btnOpen.Click += (_, _) => Accept();

        btnCancel.Text = "Zrušiť";
        btnCancel.Location = new Point(510, 286);
        btnCancel.Size = new Size(88, 32);
        btnCancel.DialogResult = DialogResult.Cancel;

        AcceptButton = btnOpen;
        CancelButton = btnCancel;
        Controls.AddRange([lblInfo, lstDatabases, btnNew, btnDelete, btnRestore, btnFolder, btnRefresh, btnOpen, btnCancel]);

        LoadItems();
    }

    private static bool IsCurrent(string path) =>
        string.Equals(path, AppDb.DbPath, StringComparison.OrdinalIgnoreCase);

    private void LoadItems(string? select = null)
    {
        var preferred = select ?? AppDb.DbPath ?? AppDb.LastUsed;

        lstDatabases.BeginUpdate();
        lstDatabases.Items.Clear();
        foreach (var path in AppDb.ListDatabases())
        {
            var info = new FileInfo(path);
            var text = $"{System.IO.Path.GetFileNameWithoutExtension(path)}   " +
                       $"({info.LastWriteTime:dd.MM.yyyy HH:mm}, {info.Length / 1024.0:N0} kB)";
            if (IsCurrent(path))
                text += "  — aktuálna";

            var index = lstDatabases.Items.Add(new Item(path, text));
            if (string.Equals(path, preferred, StringComparison.OrdinalIgnoreCase))
                lstDatabases.SelectedIndex = index;
        }
        if (lstDatabases.SelectedIndex < 0 && lstDatabases.Items.Count > 0)
            lstDatabases.SelectedIndex = 0;
        lstDatabases.EndUpdate();

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var item = lstDatabases.SelectedItem as Item;
        btnOpen.Enabled = item is not null;
        btnDelete.Enabled = item is not null && !IsCurrent(item.Path);
    }

    private void CreateNew()
    {
        var name = PromptName("Nová databáza", "Názov novej (prázdnej) databázy:", "Vytvoriť");
        if (name is null) return;

        try
        {
            var path = AppDb.Create(name);
            LoadItems(select: path);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Databázu sa nepodarilo vytvoriť:\n{ex.Message}", "Chyba",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RestoreSelectedBackup()
    {
        try
        {
            var backups = AppDb.ListBackups();
            if (backups.Count == 0)
            {
                MessageBox.Show(this, "Nie sú k dispozícii žiadne zálohy.", "Obnova zo zálohy",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new AppForm
            {
                Text = "Obnova zo zálohy",
                Font = Font,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(560, 302),
            };
            var label = new Label { Text = "Vyberte zálohu databázy.", Location = new Point(12, 12), AutoSize = true };
            var list = new ListBox
            {
                Location = new Point(12, 38), Size = new Size(536, 210),
                IntegralHeight = false, HorizontalScrollbar = true,
            };
            foreach (var backup in backups)
                list.Items.Add($"{backup.CreatedAt:dd.MM.yyyy HH:mm:ss}   {backup.DatabaseName}");

            list.SelectedIndex = 0;
            var next = new Button { Text = "Ďalej…", Location = new Point(366, 258), Size = new Size(88, 32), DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Zrušiť", Location = new Point(460, 258), Size = new Size(88, 32), DialogResult = DialogResult.Cancel };
            dlg.AcceptButton = next;
            dlg.CancelButton = cancel;
            dlg.Controls.AddRange([label, list, next, cancel]);
            if (dlg.ShowDialog(this) != DialogResult.OK || list.SelectedIndex < 0) return;

            var name = PromptName("Obnova zo zálohy", "Názov obnovenej databázy:", "Obnoviť");
            if (name is null) return;

            var path = AppDb.RestoreBackup(backups[list.SelectedIndex], name);
            LoadItems(select: path);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Databázu sa nepodarilo obnoviť:\n{ex.Message}", "Chyba",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void DeleteSelected()
    {
        if (lstDatabases.SelectedItem is not Item item) return;

        var name = System.IO.Path.GetFileNameWithoutExtension(item.Path);
        if (IsCurrent(item.Path))
        {
            MessageBox.Show(this,
                $"Databáza „{name}“ je práve otvorená a nedá sa zmazať.\n\n" +
                "Najprv otvorte inú databázu.",
                "Zmazať databázu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var counts = AppDb.GetCounts(item.Path);
        var content = counts is { } c
            ? $"Obsahuje {c.Products} výrobkov, {c.Materials} materiálov a {c.Operations} réžií.\n\n"
            : "";

        var answer = MessageBox.Show(this,
            $"Naozaj chcete natrvalo zmazať databázu „{name}“?\n\n" +
            content +
            "Súbor sa zmaže z disku (nie do koša) a túto akciu nie je možné vrátiť späť.",
            "Zmazať databázu",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;

        try
        {
            AppDb.Delete(item.Path);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Databázu sa nepodarilo zmazať:\n{ex.Message}", "Chyba",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        LoadItems();
    }

    /// <summary>Jednoduchý dialóg na zadanie názvu novej DB; null = zrušené.</summary>
    private string? PromptName(string title, string prompt, string action)
    {
        using var dlg = new AppForm
        {
            Text = title,
            Font = Font,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(360, 150),
        };
        var lbl = new Label { Text = prompt, Location = new Point(12, 12), AutoSize = true };
        var txt = new TextBox { Location = new Point(12, 38), Size = new Size(336, 27) };
        var lblError = new Label { Location = new Point(12, 70), Size = new Size(336, 34), ForeColor = Color.Firebrick };
        var ok = new Button { Text = action, Location = new Point(166, 108), Size = new Size(88, 32) };
        var cancel = new Button { Text = "Zrušiť", Location = new Point(260, 108), Size = new Size(88, 32), DialogResult = DialogResult.Cancel };

        ok.Click += (_, _) =>
        {
            var error = AppDb.ValidateNewName(txt.Text);
            if (error is null)
            {
                dlg.DialogResult = DialogResult.OK;
                return;
            }
            lblError.Text = error;
            txt.Focus();
        };

        dlg.AcceptButton = ok;
        dlg.CancelButton = cancel;
        dlg.Controls.AddRange([lbl, txt, lblError, ok, cancel]);

        return dlg.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : null;
    }

    private void Accept()
    {
        if (lstDatabases.SelectedItem is not Item item) return;
        SelectedPath = item.Path;
        DialogResult = DialogResult.OK;
        Close();
    }
}
