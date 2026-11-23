using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Kalkulacie.App;
using Kalkulacie.Core.Calculations;
using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;
using Microsoft.EntityFrameworkCore;
using TextSearch = Kalkulacie.App.TextSearch;

namespace Kalkulacie.Desktop.Pages;

/// <summary>
/// Pracovná plocha Výrobky: zoznam (hľadanie, filter typu výroby), detail so zložením a cenou
/// a pravý panel na pridanie materiálu. Všetko v jednom okne — hľadanie, filter aj výber výrobku
/// ostávajú zachované, lebo zoznam sa pri otvorení detailu len schová.
/// </summary>
public partial class ProductsPage : Page
{
    private const double ListWidth = 380;
    private const double PanelWidth = 372;
    /// <summary>Najmenšia šírka, pri ktorej sa zoznam a detail zmestia vedľa seba.</summary>
    private const double SideBySideWidth = 1000;
    /// <summary>Najmenšia šírka, pri ktorej sa vedľa seba zmestí zoznam, detail aj otvorený panel.</summary>
    private const double SideBySideWithPanelWidth = 1400;

    /// <summary>Pod touto šírkou detailu sa do tabuľky zloženia nezmestia všetky stĺpce.</summary>
    private const double CompactDetailBelow = 740;

    private Dictionary<int, Product> _products = [];
    private List<ProductListItem> _allItems = [];
    private List<MaterialItem> _allMaterials = [];
    private PricingSettings _settings = new();
    private Product? _product;

    private bool _wide = true;
    /// <summary>V úzkom okne: true = je zobrazený detail, false = zoznam.</summary>
    private bool _detailOpen;
    private bool _panelOpen;
    /// <summary>Používateľ sám prepol kusový/plošný výpočet — už ho neprepíname podľa MJ materiálu.</summary>
    private bool _kindChosen;
    private bool _updating;
    private bool _loaded;

    public ProductsPage()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        LoadProducts();
        ApplyProductFilter(null);
        UpdateLayoutMode();
        FocusLater(ProductSearch);
    }

    // ───────────── Dáta ─────────────

    /// <summary>Načíta z databázy všetky výrobky so zložením a spočíta ich ceny.</summary>
    private void LoadProducts()
    {
        using var db = AppDb.CreateContext();
        _settings = db.GetPricingSettings();
        var products = db.Products
            .Include(p => p.CompositionRows).ThenInclude(r => r.Material)
            .Include(p => p.CompositionRows).ThenInclude(r => r.Operation)
            .ToList();

        _products = products.ToDictionary(p => p.Id);
        _allItems = products
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(p => new ProductListItem(p.Id, p.Name, p.Mode, Fmt.Mode(p.Mode),
                Fmt.Money(PriceCalculator.Calculate(p, _settings).BasePrice, _settings)))
            .ToList();
    }

    /// <summary>Zobrazí výrobky podľa hľadania a filtra; ponechá (alebo nastaví) vybraný výrobok.</summary>
    private void ApplyProductFilter(int? selectId)
    {
        selectId ??= (ProductsGrid.SelectedItem as ProductListItem)?.Id;
        var term = ProductSearch.Text.Trim();
        CalculationMode? mode = FilterManual.IsChecked == true ? CalculationMode.Manual
            : FilterAutomatic.IsChecked == true ? CalculationMode.Automatic
            : null;

        var items = _allItems
            .Where(p => mode is null || p.Mode == mode)
            .Where(p => TextSearch.Matches(p.Name, term))
            .ToList();

        _updating = true;
        ProductsGrid.ItemsSource = items;
        var selected = items.FirstOrDefault(p => p.Id == selectId) ?? items.FirstOrDefault();
        ProductsGrid.SelectedItem = selected;
        _updating = false;

        if (selected is not null) ProductsGrid.ScrollIntoView(selected);
        NoProductsText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ProductCountText.Text = items.Count == _allItems.Count
            ? $"Výrobkov: {items.Count}"
            : $"Zobrazené: {items.Count} z {_allItems.Count}";

        ShowDetail(selected?.Id);
    }

    private void ShowDetail(int? productId, int? selectRowId = null)
    {
        var product = productId is int id ? _products.GetValueOrDefault(id) : null;
        bool changed = product?.Id != _product?.Id;
        _product = product;

        NoDetailText.Visibility = product is null ? Visibility.Visible : Visibility.Collapsed;
        DetailContent.Visibility = product is null ? Visibility.Collapsed : Visibility.Visible;
        if (product is null)
        {
            if (_panelOpen) ClosePanel(restoreFocus: false);
            return;
        }
        if (changed && _panelOpen) ClosePanel(restoreFocus: false);

        var rows = product.CompositionRows.OrderBy(r => r.Position).ThenBy(r => r.Id)
            .Select(r =>
            {
                bool material = r.RowType == RowType.Material;
                return new CompositionRowItem(
                    r.Id,
                    material ? "Materiál" : "Práca",
                    (material ? r.Material?.Name : r.Operation?.Name) ?? "",
                    (material ? r.Material?.Unit : null) ?? "",
                    Fmt.Number(r.Quantity),
                    Fmt.Number(r.DimensionA),
                    Fmt.Number(r.DimensionB),
                    Fmt.UnitPrice((material ? r.Material?.Price : r.Operation?.Rate) ?? 0),
                    Fmt.Money(PriceCalculator.RowCost(r, _settings), _settings));
            })
            .ToList();

        var previous = (RowsGrid.SelectedItem as CompositionRowItem)?.Id;
        RowsGrid.ItemsSource = rows;
        var selectedRow = rows.FirstOrDefault(r => r.Id == (selectRowId ?? (changed ? null : previous)))
                          ?? rows.FirstOrDefault();
        RowsGrid.SelectedItem = selectedRow;
        if (selectedRow is not null)
        {
            RowsGrid.UpdateLayout();
            RowsGrid.ScrollIntoView(selectedRow);
        }
        NoRowsText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        bool manual = product.Mode == CalculationMode.Manual;
        DetailName.Text = product.Name;
        DetailName.ToolTip = product.Name;
        DetailSubtitle.Text = $"{(manual ? "Ručná výroba" : "Strojová výroba")} · riadkov zloženia: {rows.Count}";

        var summary = PriceCalculator.Calculate(product, _settings);
        decimal margin = manual ? _settings.MarginManual : _settings.MarginAutomatic;
        MaterialTotalText.Text = Fmt.Money(summary.MaterialTotal, _settings);
        OperationTotalText.Text = Fmt.Money(summary.OperationTotal, _settings);
        UvnText.Text = Fmt.Money(summary.Uvn, _settings);
        PriceText.Text = Fmt.Money(summary.BasePrice, _settings);
        PriceCaption.Text = $"Cena (UVN × {margin.ToString("0.####", CultureInfo.CurrentCulture)})";

        var warnings = PriceWarnings.ForProduct(product).ToList();
        WarningBar.Message = string.Join("; ", warnings);
        WarningBar.IsOpen = warnings.Count > 0 && rows.Count > 0;
    }

    // ───────────── Rozloženie: vedľa seba / detail na celú plochu ─────────────

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateLayoutMode();

    private void UpdateLayoutMode()
    {
        double width = ActualWidth;
        if (width <= 0) return;

        _wide = width >= SideBySideWidth;
        // Otvorený panel berie miesto detailu; ak by naň ostalo málo, zoznam sa dočasne schová.
        bool listFits = _wide && (!_panelOpen || width >= SideBySideWithPanelWidth);
        bool showDetail = _wide || _detailOpen || _panelOpen;
        bool showList = _panelOpen ? listFits : (_wide || !_detailOpen);

        ListPane.Visibility = showList ? Visibility.Visible : Visibility.Collapsed;
        DetailPane.Visibility = showDetail ? Visibility.Visible : Visibility.Collapsed;
        MaterialPanel.Visibility = _panelOpen ? Visibility.Visible : Visibility.Collapsed;

        ListColumn.Width = !showList ? new GridLength(0)
            : showDetail ? new GridLength(ListWidth)
            : new GridLength(1, GridUnitType.Star);
        DetailColumn.Width = showDetail ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        PanelColumn.Width = _panelOpen ? new GridLength(PanelWidth + 12) : new GridLength(0);
        DetailPane.Margin = new Thickness(showList ? 12 : 0, 0, 0, 0);

        // Keď panel zúži detail, ostanú v zložení len stĺpce potrebné pri pridávaní: položka, MJ, množstvo, náklad.
        double detailWidth = width - (showList ? ListWidth + 12 : 0) - (_panelOpen ? PanelWidth + 12 : 0);
        var extra = detailWidth < CompactDetailBelow ? Visibility.Collapsed : Visibility.Visible;
        ColType.Visibility = ColDimA.Visibility = ColDimB.Visibility = ColUnitPrice.Visibility = extra;
        UvnCaption.Text = extra == Visibility.Visible ? "Vlastné náklady (UVN)" : "UVN";

        // Späť na zoznam je potrebné len vtedy, keď zoznam nie je vidieť.
        BackButton.Visibility = showList ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OpenDetail()
    {
        if (_product is null) return;
        _detailOpen = true;
        UpdateLayoutMode();
        FocusLater(RowsGrid.Items.Count > 0 ? RowsGrid : AddMaterialButton);
    }

    private void BackToList()
    {
        if (_panelOpen) ClosePanel(restoreFocus: false);
        _detailOpen = false;
        UpdateLayoutMode();
        FocusLater(ProductsGrid);
    }

    private void OnBackClick(object sender, RoutedEventArgs e) => BackToList();

    private void OnDetailGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // Pri zúžení okna ostane zobrazená tá časť, v ktorej používateľ práve pracuje.
        if (_wide) _detailOpen = true;
    }

    // ───────────── Zoznam výrobkov ─────────────

    private void OnProductSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (_loaded) ApplyProductFilter(null);
    }

    private void OnFilterChanged(object sender, RoutedEventArgs e)
    {
        if (_loaded) ApplyProductFilter(null);
    }

    private void OnProductSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating) return;
        ShowDetail((ProductsGrid.SelectedItem as ProductListItem)?.Id);
    }

    private void OnProductSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Down or Key.Up)
        {
            MoveSelection(ProductsGrid, e.Key == Key.Down ? 1 : -1);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            OpenDetail();
            e.Handled = true;
        }
    }

    private void OnProductsGridKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        OpenDetail();
        e.Handled = true;
    }

    private void OnProductsGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<DataGridRow>(e.OriginalSource as DependencyObject) is not null) OpenDetail();
    }

    private void OnProductsGridMouseUp(object sender, MouseButtonEventArgs e)
    {
        // V úzkom okne nie je detail vedľa zoznamu — kliknutie na výrobok ho otvorí.
        if (!_wide && FindParent<DataGridRow>(e.OriginalSource as DependencyObject) is not null) OpenDetail();
    }

    // ───────────── Panel: pridanie materiálu ─────────────

    private void OnAddMaterialClick(object sender, RoutedEventArgs e) => OpenPanel();

    private void OpenPanel()
    {
        if (_product is null) return;
        if (_panelOpen)
        {
            FocusLater(MaterialSearch);
            return;
        }

        using (var db = AppDb.CreateContext())
        {
            _allMaterials = db.Materials.AsNoTracking().AsEnumerable()
                .OrderBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(m => new MaterialItem(m.Id, m.Name, m.Unit ?? "", m.Price, Fmt.UnitPrice(m.Price)))
                .ToList();
        }

        _updating = true;
        MaterialSearch.Text = "";
        DimensionABox.Text = "";
        DimensionBBox.Text = "";
        QuantityBox.Text = "1";
        KindPiece.IsChecked = true;
        MaterialsGrid.SelectedItem = null;   // každé otvorenie začína prvým materiálom v zozname
        _updating = false;
        _kindChosen = false;
        PanelErrorText.Visibility = Visibility.Collapsed;
        PanelSubtitle.Text = $"do výrobku {_product.Name}";
        PanelSubtitle.ToolTip = _product.Name;

        _panelOpen = true;
        _detailOpen = true;
        UpdateLayoutMode();
        ApplyMaterialFilter();
        FocusLater(MaterialSearch);
    }

    private void ClosePanel(bool restoreFocus = true)
    {
        if (!_panelOpen) return;
        _panelOpen = false;
        UpdateLayoutMode();
        if (restoreFocus) FocusLater(AddMaterialButton);
    }

    private void OnPanelCancelClick(object sender, RoutedEventArgs e) => ClosePanel();

    private void ApplyMaterialFilter()
    {
        var selectId = (MaterialsGrid.SelectedItem as MaterialItem)?.Id;
        var term = MaterialSearch.Text.Trim();
        var items = _allMaterials.Where(m => TextSearch.Matches(m.Name, term)).ToList();

        _updating = true;
        MaterialsGrid.ItemsSource = items;
        var selected = items.FirstOrDefault(m => m.Id == selectId) ?? items.FirstOrDefault();
        MaterialsGrid.SelectedItem = selected;
        _updating = false;

        if (selected is not null) MaterialsGrid.ScrollIntoView(selected);
        NoMaterialsText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        OnMaterialChosen();
    }

    private void OnMaterialSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (!_updating && _panelOpen) ApplyMaterialFilter();
    }

    private void OnMaterialSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Down or Key.Up)) return;
        MoveSelection(MaterialsGrid, e.Key == Key.Down ? 1 : -1);
        e.Handled = true;
    }

    private void OnMaterialSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updating) OnMaterialChosen();
    }

    /// <summary>Podľa mernej jednotky predvolí plošný výpočet (m2), kým ho používateľ sám neprepne.</summary>
    private void OnMaterialChosen()
    {
        if (!_kindChosen && MaterialsGrid.SelectedItem is MaterialItem material)
        {
            bool area = material.Unit.Trim().Replace("²", "2").Equals("m2", StringComparison.OrdinalIgnoreCase);
            KindArea.IsChecked = area;
            KindPiece.IsChecked = !area;
        }
        UpdateKindFields();
        UpdatePreview();
    }

    private void OnKindClick(object sender, RoutedEventArgs e)
    {
        _kindChosen = true;
        UpdateKindFields();
        UpdatePreview();
    }

    private void UpdateKindFields()
    {
        bool area = KindArea.IsChecked == true;
        DimensionAField.Visibility = area ? Visibility.Visible : Visibility.Collapsed;
        DimensionBField.Visibility = area ? Visibility.Visible : Visibility.Collapsed;
        Grid.SetColumn(QuantityField, area ? 4 : 0);

        var unit = (MaterialsGrid.SelectedItem as MaterialItem)?.Unit;
        QuantityLabel.Text = area || string.IsNullOrWhiteSpace(unit) ? "Množstvo" : $"Množstvo ({unit.Trim()})";
    }

    private void OnRowValueChanged(object sender, TextChangedEventArgs e)
    {
        if (!_updating && _panelOpen) UpdatePreview();
    }

    private void OnNumberBoxFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // Po Tab-e sa hodnota prepíše písaním — netreba ju najprv označovať.
        if (sender is TextBox box) Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(box.SelectAll));
    }

    /// <summary>Číslo s desatinnou čiarkou alebo bodkou; null, ak text nie je číslo.</summary>
    private static decimal? ParseNumber(string text)
    {
        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        text = text.Trim().Replace(" ", "").Replace(" ", "").Replace(".", separator).Replace(",", separator);
        return decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out var value)
            ? value
            : null;
    }

    /// <summary>Nový riadok z údajov panela, alebo chyba a pole, ktoré ju spôsobilo.</summary>
    private (CompositionRow? Row, string? Error, Control? Field) ReadPanel()
    {
        if (_product is null) return (null, null, null);
        if (MaterialsGrid.SelectedItem is not MaterialItem material)
            return (null, "Vyberte materiál.", MaterialSearch);

        bool area = KindArea.IsChecked == true;
        decimal? a = null, b = null;
        if (area)
        {
            a = ParseNumber(DimensionABox.Text);
            if (a is null or <= 0) return (null, "Rozmer A musí byť číslo väčšie ako 0.", DimensionABox);
            b = ParseNumber(DimensionBBox.Text);
            if (b is null or <= 0) return (null, "Rozmer B musí byť číslo väčšie ako 0.", DimensionBBox);
        }

        var quantity = ParseNumber(QuantityBox.Text);
        if (quantity is null or <= 0) return (null, "Množstvo musí byť číslo väčšie ako 0.", QuantityBox);

        return (new CompositionRow
        {
            ProductId = _product.Id,
            Product = new Product { Id = _product.Id, Mode = _product.Mode },
            RowType = RowType.Material,
            MaterialId = material.Id,
            Material = new Material { Id = material.Id, Name = material.Name, Price = material.Price },
            DimensionA = a,
            DimensionB = b,
            Quantity = quantity.Value,
        }, null, null);
    }

    private void UpdatePreview()
    {
        PanelErrorText.Visibility = Visibility.Collapsed;
        var (row, _, _) = ReadPanel();
        if (row is null || _product is null)
        {
            PreviewCostText.Text = "—";
            PreviewPriceText.Text = "—";
            PreviewFormulaText.Text = MaterialsGrid.SelectedItem is null ? "Vyberte materiál."
                : KindArea.IsChecked == true ? "Vyplňte rozmery a množstvo."
                : "Vyplňte množstvo.";
            return;
        }

        static string N(decimal value) => value.ToString("0.####", CultureInfo.CurrentCulture);
        decimal price = row.Material!.Price;
        bool manual = _product.Mode == CalculationMode.Manual;
        PreviewFormulaText.Text = row.DimensionA is decimal a && row.DimensionB is decimal b
            ? $"{N(a)} × {N(b)} × {N(manual ? _settings.AreaMaterialManual : _settings.AreaMaterialAutomatic)} × {N(price)} × {N(row.Quantity)}"
            : $"{N(price)} × {N(row.Quantity)} × {N(_settings.PieceMaterial)}";
        PreviewCostText.Text = Fmt.Money(PriceCalculator.RowCost(row, _settings), _settings);

        // Cena výrobku s novým riadkom — tým istým výpočtom ako v detaile.
        var preview = new Product { Mode = _product.Mode, CompositionRows = [.. _product.CompositionRows, row] };
        PreviewPriceText.Text = Fmt.Money(PriceCalculator.Calculate(preview, _settings).BasePrice, _settings);
    }

    private void OnPanelSaveClick(object sender, RoutedEventArgs e) => SavePanel();

    private void SavePanel()
    {
        if (_product is null) return;
        var (row, error, field) = ReadPanel();
        if (row is null)
        {
            PanelErrorText.Text = error;
            PanelErrorText.Visibility = Visibility.Visible;
            field?.Focus();
            return;
        }

        int productId = _product.Id;
        int rowId;
        using (var db = AppDb.CreateContext())
        {
            int position = (db.CompositionRows.Where(r => r.ProductId == productId).Max(r => (int?)r.Position) ?? 0) + 1;
            var entity = new CompositionRow
            {
                ProductId = productId,
                RowType = RowType.Material,
                Position = position,
                MaterialId = row.MaterialId,
                DimensionA = row.DimensionA,
                DimensionB = row.DimensionB,
                Quantity = row.Quantity,
            };
            db.CompositionRows.Add(entity);
            db.SaveChanges();
            rowId = entity.Id;
        }

        // Všetko sa číta nanovo z databázy — zoznam (cena), zloženie aj súhrn ukazujú uložený stav.
        ClosePanel(restoreFocus: false);
        LoadProducts();
        _product = null;
        ApplyProductFilterKeeping(productId, rowId);
        FocusLater(RowsGrid);
    }

    /// <summary>Obnoví zoznam po uložení; upravený výrobok ostane vybraný, aj keby nevyhovoval filtru.</summary>
    private void ApplyProductFilterKeeping(int productId, int rowId)
    {
        ApplyProductFilter(productId);
        ShowDetail(productId, rowId);
    }

    // ───────────── Klávesnica a fokus ─────────────

    private void OnPagePreviewKeyDown(object sender, KeyEventArgs e)
    {
        bool ctrl = Keyboard.Modifiers == ModifierKeys.Control;

        if (e.Key == Key.Escape)
        {
            if (_panelOpen) ClosePanel();
            else if (ListPane.Visibility != Visibility.Visible) BackToList();
            else return;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && _panelOpen && MaterialPanel.IsKeyboardFocusWithin
                 && Keyboard.FocusedElement is not ButtonBase)
        {
            SavePanel();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.M && _product is not null)
        {
            OpenPanel();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.F)
        {
            if (ListPane.Visibility != Visibility.Visible) BackToList();
            FocusLater(ProductSearch);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Tab a Shift+Tab tabuľku opustia — DataGrid by inak chodil po bunkách riadok za riadkom.
    /// </summary>
    private void OnGridPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Tab || Keyboard.Modifiers is not (ModifierKeys.None or ModifierKeys.Shift)) return;
        var grid = (DataGrid)sender;
        bool back = Keyboard.Modifiers == ModifierKeys.Shift;

        // Ide sa od samotnej tabuľky (platí jej TabIndex); bunky nie sú zastávkami Tab,
        // takže ďalší v poradí je prvok za tabuľkou, resp. pred ňou.
        grid.MoveFocus(new TraversalRequest(back ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next));
        e.Handled = true;
    }

    /// <summary>Šípky v skupine prepínačov voľbu rovno aj vyberú (WPF by inak len presunul fokus).</summary>
    private void OnRadioGroupKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down)) return;
        var radios = ((Panel)sender).Children.OfType<RadioButton>().ToList();
        int index = radios.FindIndex(r => r.IsKeyboardFocused);
        if (index < 0) return;

        var next = radios[Math.Clamp(index + (e.Key is Key.Right or Key.Down ? 1 : -1), 0, radios.Count - 1)];
        next.IsChecked = true;
        next.Focus();
        if (next == KindPiece || next == KindArea) OnKindClick(next, e);
        e.Handled = true;
    }

    /// <summary>Po vstupe do tabuľky (Tab) dostane fokus vybraný riadok, nie prvá bunka.</summary>
    private void OnGridGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        var grid = (DataGrid)sender;
        if (e.OldFocus is DependencyObject old && IsInside(old, grid)) return;
        if (grid.SelectedItem is null) return;
        if (FindParent<DataGridRow>(e.NewFocus as DependencyObject)?.Item == grid.SelectedItem) return;
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => FocusSelectedRow(grid)));
    }

    private void FocusLater(UIElement element) =>
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            if (element is DataGrid grid) FocusSelectedRow(grid);
            else element.Focus();
        }));

    private static void FocusSelectedRow(DataGrid grid)
    {
        if (grid.SelectedItem is null || grid.Columns.Count == 0)
        {
            grid.Focus();
            return;
        }

        grid.UpdateLayout();
        grid.ScrollIntoView(grid.SelectedItem);
        grid.UpdateLayout();
        if (grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) is DataGridRow row
            && FindChild<DataGridCell>(row) is { } cell)
        {
            cell.Focus();
        }
        else
        {
            grid.Focus();
        }
    }

    private static void MoveSelection(DataGrid grid, int delta)
    {
        if (grid.Items.Count == 0) return;
        int index = Math.Clamp(grid.SelectedIndex + delta, 0, grid.Items.Count - 1);
        grid.SelectedIndex = index;
        grid.ScrollIntoView(grid.SelectedItem);
    }

    private static bool IsInside(DependencyObject element, DependencyObject ancestor)
    {
        for (DependencyObject? current = element; current is not null; current = ParentOf(current))
            if (ReferenceEquals(current, ancestor)) return true;
        return false;
    }

    private static T? FindParent<T>(DependencyObject? element) where T : DependencyObject
    {
        for (var current = element; current is not null; current = ParentOf(current))
            if (current is T match) return match;
        return null;
    }

    private static DependencyObject? ParentOf(DependencyObject element) =>
        element is Visual or System.Windows.Media.Media3D.Visual3D
            ? VisualTreeHelper.GetParent(element)
            : LogicalTreeHelper.GetParent(element);

    private static T? FindChild<T>(DependencyObject element) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            var child = VisualTreeHelper.GetChild(element, i);
            if (child is T match) return match;
            if (FindChild<T>(child) is { } nested) return nested;
        }
        return null;
    }
}
