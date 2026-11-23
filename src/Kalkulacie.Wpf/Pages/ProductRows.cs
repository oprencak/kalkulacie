using System.Globalization;
using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;

namespace Kalkulacie.Desktop.Pages;

/// <summary>Formátovanie čísel pre tabuľky — podľa nastavení systému (desatinná čiarka).</summary>
public static class Fmt
{
    /// <summary>Náklad alebo cena výrobku: toľko miest, na koľko sa zaokrúhľuje (bez zaokrúhľovania 4).</summary>
    public static string Money(decimal value, PricingSettings settings) =>
        value.ToString("N" + (settings.RoundingDecimals ?? 4), CultureInfo.CurrentCulture);

    /// <summary>Jednotková cena materiálu alebo sadzba réžie.</summary>
    public static string UnitPrice(decimal value) => value.ToString("N4", CultureInfo.CurrentCulture);

    /// <summary>Množstvo alebo rozmer — bez zbytočných núl.</summary>
    public static string Number(decimal? value) =>
        value?.ToString("#,##0.####", CultureInfo.CurrentCulture) ?? "";

    public static string Mode(CalculationMode mode) => mode == CalculationMode.Manual ? "Ručná" : "Strojová";
}

/// <summary>Riadok zoznamu výrobkov.</summary>
public sealed record ProductListItem(int Id, string Name, CalculationMode Mode, string ModeText, string Price);

/// <summary>Riadok zloženia v detaile výrobku.</summary>
public sealed record CompositionRowItem(
    int Id, string Type, string Name, string Unit, string Quantity,
    string DimensionA, string DimensionB, string UnitPrice, string Cost);

/// <summary>Materiál v paneli na pridanie riadku.</summary>
public sealed record MaterialItem(int Id, string Name, string Unit, decimal Price, string PriceText);
