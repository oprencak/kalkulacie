namespace Kalkulacie.App.Forms;

/// <summary>Spoločný predok všetkých okien aplikácie — nastavuje ikonu a jednotný vzhľad (<see cref="Theme"/>).</summary>
public class AppForm : Form
{
    public AppForm()
    {
        Icon = AppInfo.Icon;
    }

    protected override void OnLoad(EventArgs e)
    {
        // Pred Load udalosťami formulárov: tie plnia tabuľky a výška riadkov sa musí nastaviť skôr
        Theme.Apply(this);
        base.OnLoad(e);
    }
}
