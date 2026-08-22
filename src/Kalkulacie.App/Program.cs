using Kalkulacie.App.Forms;

namespace Kalkulacie.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        AppDb.EnsureDatabase();
        Application.Run(new MainForm());
    }
}