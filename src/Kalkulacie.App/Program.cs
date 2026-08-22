using System.ComponentModel;
using Kalkulacie.App.Forms;

namespace Kalkulacie.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += OnThreadException;

        ApplicationConfiguration.Initialize();
        AppDb.EnsureDatabase();
        Application.Run(new MainForm());
    }

    private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
    {
        string message = e.Exception switch
        {
            Win32Exception { NativeErrorCode: 1722 } =>
                "Tlačiareň nie je dostupná.\n\nSkontrolujte:\n" +
                "• či je nainštalovaná tlačiareň\n" +
                "• ci bezi sluzba 'Print Spooler' (tlacovy front)",
            _ => $"Nastala neočakávaná chyba:\n{e.Exception.Message}"
        };

        MessageBox.Show(message, "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
