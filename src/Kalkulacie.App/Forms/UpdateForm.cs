using Kalkulacie.App.Update;

namespace Kalkulacie.App.Forms;

/// <summary>
/// Ponuka novej verzie: poznámky k vydaniu, upozornenie na migráciu databázy a stiahnutie s priebehom.
/// Nič sa nesťahuje ani neinštaluje, kým používateľ neklikne na Aktualizovať.
/// Po úspešnej výmene exe vráti <see cref="DialogResult.OK"/> — aplikáciu treba vtedy hneď ukončiť.
/// </summary>
public class UpdateForm : AppForm
{
    private readonly ReleaseInfo _release;
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Visible = false, Height = 20 };
    private readonly Label _status = new() { Dock = DockStyle.Fill, AutoSize = true, Visible = false };
    private readonly Button _btnUpdate = new() { Text = "Aktualizovať", AutoSize = true };
    private readonly Button _btnClose = new() { Text = "Neskôr", AutoSize = true };
    private CancellationTokenSource? _cts;

    public UpdateForm(ReleaseInfo release)
    {
        _release = release;

        Text = "Aktualizácia";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 9F);
        ClientSize = new Size(520, 470);
        Padding = new Padding(12);

        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1 };
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        table.Controls.Add(new Label
        {
            Text = $"Je dostupná nová verzia {release.Version.ToString(3)} (teraz máte {AppInfo.Version}).",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
        });
        table.Controls.Add(new Label { Text = "Čo je nové:", AutoSize = true });
        table.Controls.Add(new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Text = release.Notes.Length > 0
                ? release.Notes.ReplaceLineEndings("\r\n")
                : "(bez poznámok k vydaniu)",
        });
        table.Controls.Add(new Label
        {
            Text =
                "Pozor: po aktualizácii sa databáza pri otvorení zmigruje na novú verziu a staršia verzia " +
                "programu ju už nemusí vedieť otvoriť.\r\n\r\n" +
                "Dáta v priečinku data ostanú. Pred migráciou sa databáza automaticky zálohuje do data\\zalohy. " +
                "Predchádzajúca verzia programu ostane vedľa ako Kalkulacie.App.exe.old (zmaže sa pri ďalšom " +
                "spustení). Návrat späť = vrátiť starý exe a obnoviť databázu zo zálohy v data\\zalohy.",
            AutoSize = true,
            MaximumSize = new Size(490, 0),
            BackColor = Color.FromArgb(255, 244, 206),
            Padding = new Padding(8),
            Margin = new Padding(0, 10, 0, 10),
        });
        table.Controls.Add(_progress);
        table.Controls.Add(_status);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 0),
        };
        buttons.Controls.AddRange([_btnClose, _btnUpdate]);
        table.Controls.Add(buttons);
        Controls.Add(table);

        _btnUpdate.Click += async (_, _) => await UpdateAsync();
        _btnClose.Click += (_, _) =>
        {
            if (_cts is not null) _cts.Cancel();
            else Close();
        };
        FormClosing += (_, e) =>
        {
            // Počas sťahovania sa okno nezatvára — najprv treba zrušiť
            if (_cts is not null && DialogResult != DialogResult.OK)
            {
                _cts.Cancel();
                e.Cancel = true;
            }
        };
        AcceptButton = _btnUpdate;
    }

    private async Task UpdateAsync()
    {
        _btnUpdate.Enabled = false;
        _btnClose.Text = "Zrušiť";
        _progress.Visible = _status.Visible = true;
        _status.Text = "Sťahujem…";
        _cts = new CancellationTokenSource();

        string? downloaded = null;
        try
        {
            var progress = new Progress<(long Read, long? Total)>(p =>
            {
                var mb = p.Read / 1024d / 1024d;
                if (p.Total is > 0)
                {
                    _progress.Value = (int)Math.Min(100, p.Read * 100 / p.Total.Value);
                    _status.Text = $"Sťahujem… {mb:N1} / {p.Total.Value / 1024d / 1024d:N1} MB";
                }
                else
                {
                    _progress.Style = ProgressBarStyle.Marquee;
                    _status.Text = $"Sťahujem… {mb:N1} MB";
                }
            });
            downloaded = await Updater.DownloadAsync(_release, progress, _cts.Token);

            _status.Text = "Stiahnuté a overené (SHA-256). Reštartujem…";
            _btnClose.Enabled = false;
            Refresh();
            Updater.ApplyAndRestart(downloaded);
            DialogResult = DialogResult.OK;
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            ResetAfterFailure("Sťahovanie bolo zrušené.");
        }
        catch (Exception ex)
        {
            if (downloaded is not null)
            {
                try { File.Delete(downloaded); } catch { /* skúsi sa pri ďalšom štarte */ }
            }
            ResetAfterFailure("Aktualizácia zlyhala.");
            MessageBox.Show(this, $"Aktualizácia zlyhala, program ostáva v doterajšej verzii.\n\n{Describe(ex)}",
                "Aktualizácia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ResetAfterFailure(string status)
    {
        _cts?.Dispose();
        _cts = null;
        _progress.Visible = false;
        _progress.Style = ProgressBarStyle.Blocks;
        _progress.Value = 0;
        _status.Text = status;
        _btnUpdate.Enabled = true;
        _btnClose.Enabled = true;
        _btnClose.Text = "Neskôr";
    }

    private static string Describe(Exception ex) => ex switch
    {
        UnauthorizedAccessException =>
            "Do priečinka s programom nie je možné zapisovať. Presuňte program do priečinka, kam máte prístup " +
            $"na zápis, alebo stiahnite novú verziu ručne z https://github.com/{Updater.Repo}/releases.\n\n{ex.Message}",
        HttpRequestException =>
            $"Nepodarilo sa stiahnuť súbor — skontrolujte pripojenie na internet.\n\n{ex.Message}",
        _ => ex.Message,
    };
}
