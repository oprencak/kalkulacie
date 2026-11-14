namespace Kalkulacie.App.Forms;

/// <summary>Spoločný predok všetkých okien aplikácie — nastavuje ikonu.</summary>
public class AppForm : Form
{
    public AppForm()
    {
        Icon = AppInfo.Icon;
    }
}
