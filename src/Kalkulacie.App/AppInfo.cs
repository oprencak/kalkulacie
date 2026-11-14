using System.Reflection;

namespace Kalkulacie.App;

/// <summary>Verzia a ikona aplikácie. Verzia sa nastavuje v Kalkulacie.App.csproj (&lt;Version&gt;).</summary>
public static class AppInfo
{
    /// <summary>Verzia v tvare 1.2.3.</summary>
    public static string Version { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";

    /// <summary>Ikona palmy (palma.ico vložená do zostavy) — spoločná pre všetky okná.</summary>
    public static Icon Icon { get; } = LoadIcon();

    private static Icon LoadIcon()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Kalkulacie.App.palma.ico");
        return stream is null ? SystemIcons.Application : new Icon(stream);
    }
}
