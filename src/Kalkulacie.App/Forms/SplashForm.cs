namespace Kalkulacie.App.Forms;

/// <summary>
/// Úvodná obrazovka počas štartu (otvorenie databázy, záloha, migrácie, príprava hlavného okna).
/// Všetko si kreslí sama v OnPaint — počas štartu beží práca na UI vlákne, takže detské ovládacie
/// prvky by sa nemuseli stihnúť vykresliť.
/// </summary>
public class SplashForm : AppForm
{
    private readonly Bitmap _logo = new Icon(AppInfo.Icon, 96, 96).ToBitmap();
    private readonly Font _titleFont = new("Segoe UI", 20F, FontStyle.Bold);
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

        g.DrawImage(_logo, 28, (ClientSize.Height - 96) / 2, 96, 96);

        const int textX = 148;
        g.DrawString("Kalkulácie", _titleFont, Brushes.Black, textX - 4, 40);
        g.DrawString($"Verzia {AppInfo.Version}", _textFont, Brushes.Gray, textX, 82);
        g.DrawString(_status, _textFont, Brushes.DimGray, textX, 112);

        using var border = new Pen(Color.FromArgb(180, 180, 180));
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
