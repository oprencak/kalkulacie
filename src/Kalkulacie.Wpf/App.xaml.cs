using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Kalkulacie.App;
using Wpf.Ui.Appearance;

namespace Kalkulacie.Desktop;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        // Rovnaká zelená ako vo WinForms aplikácii (Theme.Accent)
        ApplicationAccentColorManager.Apply(Color.FromRgb(14, 124, 107), ApplicationTheme.Light);

        try
        {
            AppDb.Open(ChooseDatabase(e.Args));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Databázu sa nepodarilo otvoriť:\n{ex.Message}", "Kalkulácie",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown(1);
            return;
        }

        new MainWindow().Show();
    }

    /// <summary>
    /// <c>--db cesta</c> otvorí zadaný súbor. Inak sa berie priečinok <c>data</c> vedľa exe: naposledy použitá DB,
    /// prvá nájdená, alebo sa vytvorí nová. Výber databázy v okne nie je súčasťou prototypu.
    /// </summary>
    private static string ChooseDatabase(string[] args)
    {
        int index = Array.IndexOf(args, "--db");
        if (index >= 0 && index + 1 < args.Length)
        {
            var path = Path.GetFullPath(args[index + 1]);
            if (!File.Exists(path)) throw new FileNotFoundException($"Súbor {path} neexistuje.");
            return path;
        }

        AppDb.PrepareDataDir();
        return AppDb.LastUsed
               ?? AppDb.ListDatabases().FirstOrDefault()
               ?? Path.Combine(AppDb.DataDir, AppDb.DefaultDbFileName);
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Nastala neočakávaná chyba:\n{e.Exception.Message}", "Kalkulácie",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }
}
