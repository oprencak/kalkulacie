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
        var dbPath = ChooseDatabaseOnStartup();
        if (dbPath is null) return;

        AppDb.Open(dbPath);
        Application.Run(new MainForm());
    }

    /// <summary>
    /// 0 databáz → vytvorí sa nová kalkulacie.db, 1 → použije sa, viac → výber v okne.
    /// Vráti null, ak používateľ výber zrušil.
    /// </summary>
    private static string? ChooseDatabaseOnStartup()
    {
        AppDb.PrepareDataDir();
        var databases = AppDb.ListDatabases();

        switch (databases.Count)
        {
            case 0:
                return Path.Combine(AppDb.DataDir, AppDb.DefaultDbFileName);
            case 1:
                return databases[0];
            default:
                using (var picker = new DatabasePickerForm())
                    return picker.ShowDialog() == DialogResult.OK ? picker.SelectedPath : null;
        }
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
