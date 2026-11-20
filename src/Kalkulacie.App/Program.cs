using System.ComponentModel;
using Kalkulacie.App.Forms;
using Kalkulacie.App.Update;

namespace Kalkulacie.App;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += OnThreadException;

        ApplicationConfiguration.Initialize();

        // Úvodná obrazovka čo najskôr — otvorenie DB (záloha, migrácie) a prvé okno chvíľu trvajú
        using var splash = new SplashForm();
        splash.ShowStatus("Spúšťam…");

        // Po aktualizácii počkať, kým skončí predchádzajúca verzia (drží otvorenú databázu)
        bool restartAfterUpdate = Updater.IsRestartAfterUpdate(args);
        if (restartAfterUpdate)
        {
            splash.ShowStatus("Dokončujem aktualizáciu…");
            Updater.WaitForPreviousInstance(args);
        }

#if DEBUG
        // Profil „Test“: vlastná databáza so vzorovými dátami namiesto výberu DB
        bool testData = args.Contains(TestData.Argument);
        if (testData) AppDb.PrepareDataDir();
        var dbPath = testData ? TestData.DbPath : ChooseDatabaseOnStartup(splash);
#else
        var dbPath = ChooseDatabaseOnStartup(splash);
#endif
        if (dbPath is null) return;

        splash.ShowStatus($"Otváram databázu {Path.GetFileNameWithoutExtension(dbPath)}…");
        AppDb.Open(dbPath);
#if DEBUG
        if (testData) TestData.SeedIfEmpty();
#endif

        var mainForm = new MainForm();
        mainForm.Shown += (_, _) =>
        {
            splash.Close();
            Updater.CleanupAfterSuccessfulStart(restartAfterUpdate);
        };
        Application.Run(mainForm);
    }

    /// <summary>
    /// 0 databáz → vytvorí sa nová kalkulacie.db, 1 → použije sa, viac → výber v okne.
    /// Vráti null, ak používateľ výber zrušil.
    /// </summary>
    private static string? ChooseDatabaseOnStartup(SplashForm splash)
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
                {
                    // Počas výberu úvodnú obrazovku schovať, aby neprekážala. Schovať ju treba PRED
                    // zobrazením výberu — inak by sa stala vlastníkom dialógu a jej schovanie by ho ovplyvnilo.
                    splash.Hide();
                    return picker.ShowDialog() == DialogResult.OK ? picker.SelectedPath : null;
                }
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
