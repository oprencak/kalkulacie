namespace Kalkulacie.Core.Entities;

/// <summary>
/// Koeficienty cenových vzorcov — jediný riadok (Id = 1) v každej databáze.
/// Predvolené hodnoty zodpovedajú pôvodnému FoxPro KALK.PRG.
/// </summary>
public class PricingSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    /// <summary>Plošný materiál, ručná výroba (A × B × koef × cena × množstvo).</summary>
    public decimal AreaMaterialManual { get; set; } = 1.15m;

    /// <summary>Plošný materiál, strojová výroba.</summary>
    public decimal AreaMaterialAutomatic { get; set; } = 1.05m;

    /// <summary>Kusový materiál (cena × množstvo × koef).</summary>
    public decimal PieceMaterial { get; set; } = 1.03m;

    /// <summary>Cena = UVN × koef, ručná výroba.</summary>
    public decimal MarginManual { get; set; } = 1.15m;

    /// <summary>Cena = UVN × koef, strojová výroba.</summary>
    public decimal MarginAutomatic { get; set; } = 1.08m;

    /// <summary>
    /// Počet desatinných miest, na ktoré sa zaokrúhľuje každý riadok zloženia aj každá výsledná cena
    /// (FoxPro: 3). Null = nezaokrúhľovať.
    /// </summary>
    public int? RoundingDecimals { get; set; } = 3;

    /// <summary>Zaokrúhli hodnotu podľa nastavenia; polovice od nuly, rovnako ako FoxPro ROUND().</summary>
    public decimal Round(decimal value) =>
        RoundingDecimals is int decimals
            ? Math.Round(value, decimals, MidpointRounding.AwayFromZero)
            : value;
}
