using System.Globalization;

namespace Kalkulacie.App;

public static class TextSearch
{
    /// <summary>Hľadanie podreťazca bez ohľadu na veľkosť písmen a diakritiku. Prázdny výraz vyhovuje vždy.</summary>
    public static bool Matches(string? text, string term) =>
        term.Length == 0 ||
        CultureInfo.CurrentCulture.CompareInfo.IndexOf(
            text ?? "", term, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
}
