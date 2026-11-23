using System.Windows;
using Kalkulacie.App;
using Kalkulacie.Desktop.Pages;
using Wpf.Ui.Controls;

namespace Kalkulacie.Desktop;

public partial class MainWindow : FluentWindow
{
    /// <summary>Pod touto šírkou okna sa ľavá navigácia zbalí na ikony, aby ostalo miesto na prácu.</summary>
    private const double CompactNavigationBelow = 1340;

    private bool? _paneOpen;

    public MainWindow()
    {
        InitializeComponent();
        Title = $"Kalkulácie — {AppDb.DbName}";
        AppTitleBar.Title = Title;
    }

    private void OnNavLoaded(object sender, RoutedEventArgs e)
    {
        Nav.Navigate(typeof(ProductsPage));
        UpdateNavigationPane();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateNavigationPane();

    private void UpdateNavigationPane()
    {
        // Prepína sa len pri prekročení hranice — ručné zbalenie/rozbalenie používateľom inak ostáva.
        bool open = ActualWidth >= CompactNavigationBelow;
        if (_paneOpen == open) return;
        _paneOpen = open;
        Nav.IsPaneOpen = open;
    }
}
