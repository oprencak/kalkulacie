<#
.SYNOPSIS
    Vydanie novej verzie: publish → git tag vX.Y.Z → GitHub Release s Kalkulacie.App.exe.

.DESCRIPTION
    Verzia sa berie z <Version> v src\Kalkulacie.App\Kalkulacie.App.csproj — pred spustením ju zvýšte
    a zmenu commitnite. Skript:
      1. overí gh CLI a prihlásenie, čistý pracovný strom, vetvu master a že tag ešte neexistuje,
      2. vyčistí publish\ (okrem priečinka data\) a spustí dotnet publish,
      3. vypočíta SHA-256 exe a vypíše súhrn; po potvrdení
      4. pushne master,
      5. vytvorí GitHub Release (a s ním tag vX.Y.Z na HEAD) s exe a súborom .sha256 (poznámky = -Notes, inak commity od minulého tagu).

    Aplikácia si pri štarte nájde posledný release a ponúkne aktualizáciu (src\Kalkulacie.App\Update\Updater.cs).

.EXAMPLE
    .\release.ps1
.EXAMPLE
    .\release.ps1 -DryRun      # len publish a súhrn, bez pushu a release
.EXAMPLE
    .\release.ps1 -Notes "- Oprava tlače cenníka"
#>
param(
    # Poznámky k vydaniu (zobrazia sa v aplikácii). Bez nich sa použijú správy commitov od minulého tagu.
    [string]$Notes,
    # Nepýtať sa na potvrdenie pred pushom a vytvorením release.
    [switch]$Yes,
    # Len skúška: overenia, publish a súhrn — nič sa nepushne ani nevytvorí. Necommitnuté zmeny sú len varovanie.
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csproj = Join-Path $root 'src\Kalkulacie.App\Kalkulacie.App.csproj'
$publishDir = Join-Path $root 'publish'
$exe = Join-Path $publishDir 'Kalkulacie.App.exe'
$repo = 'oprencak/kalkulacie'

function Fail([string]$message) { Write-Host "CHYBA: $message" -ForegroundColor Red; exit 1 }
function Invoke-Checked([string]$what, [scriptblock]$cmd) {
    & $cmd
    if ($LASTEXITCODE -ne 0) { Fail "$what zlyhalo (kód $LASTEXITCODE)." }
}

# --- gh CLI -------------------------------------------------------------------------------------
$gh = Get-Command gh -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1
if (-not $gh -and (Test-Path 'C:\Program Files\GitHub CLI\gh.exe')) { $gh = 'C:\Program Files\GitHub CLI\gh.exe' }
if (-not $gh) {
    Fail ("Nenašiel som gh (GitHub CLI). Nainštalujte ho: winget install --id GitHub.cli`n" +
          "potom otvorte nový terminál a prihláste sa: gh auth login")
}
& $gh auth status *> $null
if ($LASTEXITCODE -ne 0) { Fail 'gh nie je prihlásené. Spustite: gh auth login' }

# --- Verzia a stav repa --------------------------------------------------------------------------
$version = ([xml](Get-Content $csproj -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if ($version -notmatch '^\d+\.\d+\.\d+$') { Fail "Neplatná <Version> v csproj: '$version' (očakávam X.Y.Z)." }
$tag = "v$version"

Push-Location $root
try {
    $branch = git rev-parse --abbrev-ref HEAD
    if ($branch -ne 'master') { Fail "Vydáva sa z vetvy master, teraz ste na '$branch'." }
    if (git status --porcelain) {
        if ($DryRun) { Write-Host 'VAROVANIE: pracovný strom nie je čistý (pri ostrom vydaní je to chyba).' -ForegroundColor Yellow }
        else { Fail 'Pracovný strom nie je čistý — najprv commitnite zmeny (aj zvýšenie verzie).' }
    }

    Invoke-Checked 'git fetch' { git fetch --tags origin --quiet }
    # Tag vytvára až GitHub spolu s release. Existujúci tag je v poriadku len vtedy, keď ukazuje na HEAD
    # a release k nemu ešte nie je (dokončenie predchádzajúceho nepodareného pokusu).
    $head = git rev-parse HEAD
    $tagExists = [bool](git tag --list $tag)
    if ($tagExists) {
        if ((git rev-list -n 1 $tag) -ne $head) { Fail "Tag $tag už existuje na inom commite — zvýšte <Version> v $csproj." }
        & $gh release view $tag -R $repo *> $null
        if ($LASTEXITCODE -eq 0) { Fail "Release $tag už existuje — zvýšte <Version> v $csproj." }
        Write-Host "Tag $tag už existuje na HEAD, release k nemu nie je — dokončím ho." -ForegroundColor Yellow
    }

    $lastTag = git tag --list 'v*' --sort=-v:refname | Where-Object { $_ -ne $tag } | Select-Object -First 1
    if ($lastTag -and ([version]$version -le [version]$lastTag.TrimStart('v'))) {
        Fail "Verzia $version nie je vyššia ako posledný tag $lastTag."
    }

    if (-not $Notes) {
        $range = if ($lastTag) { "$lastTag..HEAD" } else { 'HEAD' }
        $Notes = (git log $range --no-merges --format='- %s') -join "`n"
    }

    # --- Publish -------------------------------------------------------------------------------
    if (Get-Process Kalkulacie.App -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "$publishDir\*" }) {
        Fail 'Aplikácia z priečinka publish beží — zatvorte ju.'
    }
    # Staré súbory z predchádzajúcich publishov (dll, pdb…) preč; priečinok data (ostré databázy) ostáva
    if (Test-Path $publishDir) {
        Get-ChildItem $publishDir -Force | Where-Object { $_.Name -ne 'data' } | Remove-Item -Recurse -Force
    }
    Invoke-Checked 'dotnet publish' { dotnet publish $csproj -c Release -o $publishDir }

    $extra = Get-ChildItem $publishDir -Force | Where-Object { $_.Name -notin 'data', 'Kalkulacie.App.exe' }
    if ($extra) { Fail "V publish\ sú okrem exe aj ďalšie súbory: $($extra.Name -join ', ')" }

    $hash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    $shaFile = Join-Path ([IO.Path]::GetTempPath()) 'Kalkulacie.App.exe.sha256'
    Set-Content $shaFile "$hash  Kalkulacie.App.exe" -NoNewline -Encoding ascii
    $notesFile = Join-Path ([IO.Path]::GetTempPath()) "kalkulacie-$tag-notes.md"
    Set-Content $notesFile $Notes -Encoding utf8

    # --- Súhrn a potvrdenie ----------------------------------------------------------------------
    Write-Host ''
    Write-Host "Vydanie $tag ($repo)" -ForegroundColor Cyan
    Write-Host ("  exe:     {0} ({1:N1} MB)" -f $exe, ((Get-Item $exe).Length / 1MB))
    Write-Host "  SHA-256: $hash"
    Write-Host "  commit:  $(git log -1 --format='%h %s')"
    Write-Host '  poznámky:'
    $Notes -split "`n" | ForEach-Object { Write-Host "    $_" }
    Write-Host ''
    if ($DryRun) { Write-Host 'DryRun: nič sa nepushlo ani nevytvorilo.' -ForegroundColor Yellow; exit 0 }
    if (-not $Yes) {
        $answer = Read-Host 'Pushnúť master, vytvoriť tag a GitHub Release? (a/n)'
        if ($answer -notin 'a', 'ano', 'áno', 'y', 'yes') { Write-Host 'Zrušené, nič sa nepushlo.'; exit 0 }
    }

    # --- Tag a release -----------------------------------------------------------------------------
    Invoke-Checked 'git push master' { git push origin master }
    # Tag vytvorí GitHub spolu s release (--target) — ak release zlyhá (napr. token bez práv), nevznikne
    # osamotený tag a skript sa dá jednoducho spustiť znova.
    Invoke-Checked 'gh release create' {
        & $gh release create $tag $exe $shaFile -R $repo --title "Verzia $version" --notes-file $notesFile --target $head
    }
    Invoke-Checked 'git fetch tag' { git fetch --tags origin --quiet }

    Write-Host ''
    Write-Host "Hotovo: https://github.com/$repo/releases/tag/$tag" -ForegroundColor Green
}
finally {
    Pop-Location
}
