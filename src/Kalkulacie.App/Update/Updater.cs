using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Kalkulacie.App.Update;

/// <summary>Novšia verzia nájdená na GitHube.</summary>
public sealed record ReleaseInfo(Version Version, string Tag, string Notes, Uri DownloadUrl, string Sha256);

/// <summary>
/// Aktualizácia z GitHub Releases (repo <see cref="Repo"/>). Release má tag vX.Y.Z a súbor
/// <see cref="AssetName"/>; jeho SHA-256 sa berie z poľa "digest" v GitHub API, prípadne zo súboru
/// <c>Kalkulacie.App.exe.sha256</c> v tom istom release.
///
/// Postup: stiahnuť do <c>&lt;exe&gt;.new</c> → overiť SHA-256 → bežiaci exe premenovať na <c>.old</c> →
/// nový dať na jeho miesto → spustiť ho. Nový proces počká na skončenie starého; <c>.old</c> sa zmaže až pri
/// ďalšom úspešnom štarte (zobrazení hlavného okna) po tom prvom. Priečinok data\ sa nemení.
/// </summary>
public static partial class Updater
{
    public const string Repo = "oprencak/kalkulacie";
    public const string AssetName = "Kalkulacie.App.exe";

    /// <summary>Argument pre nový proces po aktualizácii: <c>--po-aktualizacii &lt;pid starého procesu&gt;</c>.</summary>
    public const string RestartArg = "--po-aktualizacii";

    private static readonly Uri LatestReleaseApi = new($"https://api.github.com/repos/{Repo}/releases/latest");
    private const string DownloadBase = $"https://github.com/{Repo}/releases/download/";

    private static string? ExePath => Environment.ProcessPath;

    /// <summary>
    /// Aktualizovať sa dá len publikovaný jednosúborový exe (vedľa neho nie je Kalkulacie.App.dll).
    /// Pri spustení z Visual Studia / dotnet run sa kontrola nerobí.
    /// </summary>
    public static bool IsSupported =>
        ExePath is not null
        && string.Equals(Path.GetFileName(ExePath), AssetName, StringComparison.OrdinalIgnoreCase)
        && !File.Exists(Path.Combine(AppContext.BaseDirectory, "Kalkulacie.App.dll"));

    public static Version CurrentVersion { get; } = Version.Parse(AppInfo.Version);

    [GeneratedRegex(@"^v(\d+)\.(\d+)\.(\d+)$")]
    private static partial Regex TagRegex();

    [GeneratedRegex("^[0-9a-fA-F]{64}$")]
    private static partial Regex Sha256Regex();

    private static HttpClient CreateClient(TimeSpan timeout)
    {
        var http = new HttpClient { Timeout = timeout };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Kalkulacie", AppInfo.Version));
        return http;
    }

    /// <summary>
    /// Zistí posledný release. Vráti ho, len ak je novší ako bežiaca verzia; inak, aj pri akejkoľvek chybe
    /// (bez internetu, limit GitHub API, zlý formát), vráti null.
    /// </summary>
    public static async Task<ReleaseInfo?> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            if (!IsSupported) return null;

            using var http = CreateClient(TimeSpan.FromSeconds(15));
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var response = await http.GetAsync(LatestReleaseApi, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            var root = doc.RootElement;

            var tag = root.GetProperty("tag_name").GetString() ?? "";
            var match = TagRegex().Match(tag);
            if (!match.Success) return null;
            var version = new Version(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value),
                int.Parse(match.Groups[3].Value));
            if (version <= CurrentVersion) return null;

            string? sha256 = null;
            bool hasAsset = false;
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                if (asset.GetProperty("name").GetString() != AssetName) continue;
                hasAsset = true;
                if (asset.TryGetProperty("digest", out var digest) && digest.GetString() is { } d
                    && d.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                    sha256 = d["sha256:".Length..];
            }
            if (!hasAsset) return null;

            // Adresa sa skladá z pevného repa a overeného tagu — nie z URL, ktorú vráti API
            var downloadUrl = new Uri(DownloadBase + tag + "/" + AssetName);
            sha256 ??= await DownloadSha256FileAsync(http, tag, ct).ConfigureAwait(false);
            if (sha256 is null || !Sha256Regex().IsMatch(sha256)) return null;

            var notes = root.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "";
            return new ReleaseInfo(version, tag, notes.Trim(), downloadUrl, sha256.ToLowerInvariant());
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> DownloadSha256FileAsync(HttpClient http, string tag, CancellationToken ct)
    {
        using var response = await http.GetAsync(DownloadBase + tag + "/" + AssetName + ".sha256", ct)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;
        var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }

    /// <summary>
    /// Stiahne nový exe do <c>&lt;exe&gt;.new</c> a overí jeho SHA-256. Vráti cestu k stiahnutému súboru.
    /// Pri chybe, zrušení alebo nezhode SHA-256 súbor zmaže a vyhodí výnimku.
    /// </summary>
    public static async Task<string> DownloadAsync(ReleaseInfo release, IProgress<(long Read, long? Total)> progress,
        CancellationToken ct)
    {
        var target = ExePath + ".new";
        try
        {
            using var http = CreateClient(TimeSpan.FromMinutes(30));
            using var response = await http.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            if (response.RequestMessage?.RequestUri?.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("Sťahovanie neprebehlo cez HTTPS.");

            var total = response.Content.Headers.ContentLength;
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var source = await response.Content.ReadAsStreamAsync(ct))
            await using (var file = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                long read = 0;
                int n;
                while ((n = await source.ReadAsync(buffer, ct)) > 0)
                {
                    hash.AppendData(buffer, 0, n);
                    await file.WriteAsync(buffer.AsMemory(0, n), ct);
                    read += n;
                    progress.Report((read, total));
                }
            }

            var actual = Convert.ToHexStringLower(hash.GetHashAndReset());
            if (actual != release.Sha256)
                throw new InvalidDataException(
                    "Stiahnutý súbor nesedí s kontrolným súčtom SHA-256 z GitHubu — aktualizácia sa nepoužije.\n\n" +
                    $"Očakávaný: {release.Sha256}\nStiahnutý: {actual}");
            return target;
        }
        catch
        {
            TryDelete(target);
            throw;
        }
    }

    /// <summary>
    /// Bežiaci exe premenuje na .old, stiahnutý dá na jeho miesto a spustí ho. Po návrate treba aplikáciu
    /// hneď ukončiť. Pri chybe vráti pôvodný stav a vyhodí výnimku.
    /// </summary>
    public static void ApplyAndRestart(string downloadedFile)
    {
        var exe = ExePath ?? throw new InvalidOperationException("Neznáma cesta k programu.");
        var old = exe + ".old";

        if (File.Exists(old)) File.Delete(old);
        File.Move(exe, old);
        try
        {
            File.Move(downloadedFile, exe);
        }
        catch
        {
            File.Move(old, exe);
            throw;
        }

        try
        {
            var start = new ProcessStartInfo(exe)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(exe)!,
            };
            start.ArgumentList.Add(RestartArg);
            start.ArgumentList.Add(Environment.ProcessId.ToString());
            Process.Start(start);
        }
        catch
        {
            // Nový exe sa nedá spustiť — vrátiť pôvodný
            File.Move(exe, downloadedFile, overwrite: true);
            File.Move(old, exe);
            throw;
        }
    }

    /// <summary>
    /// Ak bol program spustený po aktualizácii, počká, kým skončí starý proces (drží databázu).
    /// Volá sa na začiatku štartu, pred otvorením DB.
    /// </summary>
    public static void WaitForPreviousInstance(string[] args)
    {
        var i = Array.IndexOf(args, RestartArg);
        if (i < 0 || i + 1 >= args.Length || !int.TryParse(args[i + 1], out var pid)) return;
        try
        {
            using var previous = Process.GetProcessById(pid);
            previous.WaitForExit(TimeSpan.FromSeconds(30));
        }
        catch
        {
            // Proces už neexistuje
        }
    }

    /// <summary>Bol tento proces spustený hneď po aktualizácii?</summary>
    public static bool IsRestartAfterUpdate(string[] args) => args.Contains(RestartArg);

    /// <summary>
    /// Volá sa po úspešnom štarte (zobrazené hlavné okno): zmaže nedokončené .new a predchádzajúcu verziu .old.
    /// Pri prvom štarte hneď po aktualizácii sa .old nechá — počas tohto behu sa ešte dá vrátiť späť
    /// (zavrieť aplikáciu, zmazať exe a .old premenovať naspäť).
    /// </summary>
    public static void CleanupAfterSuccessfulStart(bool restartAfterUpdate)
    {
        if (ExePath is null) return;
        TryDelete(ExePath + ".new");
        if (!restartAfterUpdate)
            TryDelete(ExePath + ".old");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Nevadí — skúsi sa pri ďalšom štarte
        }
    }
}
