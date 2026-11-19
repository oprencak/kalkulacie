namespace Kalkulacie.App.Forms;

/// <summary>
/// Úvodná obrazovka počas štartu (otvorenie databázy, záloha, migrácie, príprava hlavného okna).
/// Všetko si kreslí sama v OnPaint — počas štartu beží práca na UI vlákne, takže detské ovládacie
/// prvky by sa nemuseli stihnúť vykresliť.
/// </summary>
public class SplashForm : AppForm
{
    private readonly Bitmap _logo = new Icon(AppInfo.Icon, 96, 96).ToBitmap();
    private readonly Font _titleFont = new("Segoe UI Semibold", 20F);
    private readonly Font _textFont = new("Segoe UI", 9.75F);
    private string _status = "Spúšťam…";

    public SplashForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        BackColor = Color.White;
        ClientSize = new Size(420, 170);
        DoubleBuffered = true;
        Text = "Kalkulácie";
    }

    /// <summary>Zobrazí (alebo znovu zobrazí) obrazovku so zadaným textom a hneď ju vykreslí.</summary>
    public void ShowStatus(string status)
    {
        _status = status;
        if (!Visible) Show();
        Refresh();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(Theme.Surface);

        g.DrawImage(_logo, 28, (ClientSize.Height - 96) / 2, 96, 96);

        const int textX = 148;
        using var text = new SolidBrush(Theme.Text);
        using var muted = new SolidBrush(Theme.TextMuted);
        g.DrawString("Kalkulácie", _titleFont, text, textX - 4, 38);
        g.DrawString($"Verzia {AppInfo.Version}", _textFont, muted, textX, 80);
        g.DrawString(_status, _textFont, muted, textX, 110);

        // Farebný pásik dole a tenký rámik okolo
        using var accent = new SolidBrush(Theme.Accent);
        g.FillRectangle(accent, 0, ClientSize.Height - 5, ClientSize.Width, 5);
        using var border = new Pen(Theme.Border);
        g.DrawRectangle(border, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logo.Dispose();
            _titleFont.Dispose();
            _textFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
