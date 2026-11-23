# Kalkulacie

Prepis starej FoxPro aplikácie (C:\dev\duri) do .NET WinForms.
Pôvodná aplikácia: cenník/kalkulačný systém pre výrobnú firmu, v prevádzke od roku 1997.

## Čo aplikácia robí

Kalkulácia nákladovej ceny výrobku zo zloženia (BOM — bill of materials):
- Evidencia **materiálov** (vstupy, suroviny) s jednotkovými cenami
- Evidencia **réžií / práce** (operácie) so sadzbami
- Evidencia **výrobkov** v dvoch módoch: `Manual` (ručná výroba) a `Automatic` (strojová výroba)
- Každý výrobok má **zloženie** — zoznam riadkov (materiál alebo réžia)
- Automatický výpočet ceny podľa vzorcov z pôvodného FoxPro

## Technický stack

| | |
|---|---|
| UI | WinForms (.NET 10, `net10.0-windows`; SDK v `global.json`) |
| Databáza | SQLite — `*.db` v priečinku `data\` vedľa `Kalkulacie.App.exe` (viac DB → výber pri štarte, zmena v hlavnom menu; `AppDb.cs`) |
| ORM | EF Core 10 (code-first, inkrementálne migrácie; verzie balíkov pevné, nie `*`) |
| Distribúcia | jeden samostatný exe (self-contained, bez inštalácie), aktualizácia z GitHub Releases |

Projekty:
- `src/Kalkulacie.Core` — entity, DbContext, cenové vzorce
- `src/Kalkulacie.App` — WinForms formuláre
- `src/Kalkulacie.Import` — konzolový import z FoxPro DBF do novej SQLite DB (vlastný DBF reader v `Dbf/`)
- `src/Kalkulacie.Wpf` — prototyp nového UI (WPF + WPF UI od Lepoco), beží vedľa WinForms aplikácie nad tým istým
  `Kalkulacie.Core`; zatiaľ len tok Výrobky → detail → pridanie materiálu. Menný priestor je `Kalkulacie.Desktop`
  (`Kalkulacie.Wpf` koliduje s `Wpf.Ui` v generovanom kóde XAML). `AppDb.cs` a `TextSearch.cs` sú linkované
  z `Kalkulacie.App`. Spustenie: `dotnet run --project src/Kalkulacie.Wpf -- --db <cesta k .db>`
  (bez `--db` sa berie `data\` vedľa exe). Snímky v `docs/wpf-prototyp` sú nad vymyslenými dátami.

Zálohy: pri každom otvorení DB sa pred migráciou uloží kópia do `data\zalohy\<db>_<yyyyMMdd_HHmmss>.db`
(len ak sa DB od poslednej zálohy zmenila; drží sa posledných 10 na databázu) — `AppDb.Backup`.
`Migrate()` sa volá len pri čakajúcich migráciách — od EF Core 9 totiž vždy zapíše do DB (zámok migrácií),
čím by sa zmenil čas súboru a pri každom štarte by vznikla nová záloha.

## Databázové entity (čisté, bez legacy polí)

```
Material    Id, Name, Unit?, Price
Operation   Id, Name, Rate
Product     Id, Name, Mode (Manual/Automatic)
CompositionRow  Id, ProductId, RowType, Position, MaterialId?, DimensionA?, DimensionB?,
                OperationId?, Quantity
PricingSettings Id (=1), AreaMaterialManual, AreaMaterialAutomatic, PieceMaterial,
                MarginManual, MarginAutomatic, RoundingDecimals?
```

- `Position` — poradie riadku v zložení výrobku (1..n); mení sa tlačidlami Hore/Dole v detaile výrobku.
- `Quantity` pri réžii znamená **počet kusov**, ktorým sa delí sadzba (vo FoxPro `POCET_KS`, „Počet kusov“).

Migrácie: `src/Kalkulacie.Core/Migrations/`

## Cenové vzorce (z FoxPro KALK.PRG)

```
R(x) = zaokrúhlenie na RoundingDecimals miest, polovice od nuly (null = bez zaokrúhlenia)

Materiál — plošný (DimensionA != 0 && DimensionB != 0):
  Manual:     cost = R(A × B × AreaMaterialManual    × Price × Qty)     [1.15]
  Automatic:  cost = R(A × B × AreaMaterialAutomatic × Price × Qty)     [1.05]

Materiál — kusový:
  cost = R(Price × Qty × PieceMaterial)                                 [1.03]

Réžia/práca:
  cost = R(Rate / Qty)

Súhrn:
  UVN = R(sum(material_costs) + sum(operation_costs))
  Manual:     BasePrice = R(UVN × MarginManual)                         [1.15]
  Automatic:  BasePrice = R(UVN × MarginAutomatic)                      [1.08]
```

Koeficienty a zaokrúhľovanie nie sú v kóde natvrdo — sú v tabuľke `PricingSettings` (jediný riadok, Id = 1,
v každej databáze zvlášť). V hranatých zátvorkách sú predvolené hodnoty z FoxPro; zaokrúhľuje sa predvolene
na 3 miesta, rovnako ako `ROUND(...,3)` v origináli. Načítanie: `db.GetPricingSettings()`.

Implementácia: `src/Kalkulacie.Core/Calculations/PriceCalculator.cs`
(`RowCost` = náklad jedného riadku, `Calculate` = súhrn výrobku; obe berú `PricingSettings`)

Ceny sa neukladajú, vždy sa počítajú — zmena koeficientu sa hneď prejaví vo všetkých výrobkoch.
Výrobok má jedinú cenu (`BasePrice`). Pôvodné FoxPro ceny „+8 %“ a „+16 %“ boli zámerne zrušené —
inú prirážku si používateľ nastaví v koeficientoch.

## WinForms formuláre

| Formulár | Súbor |
|---|---|
| Úvodná obrazovka pri štarte | `Forms/SplashForm` |
| Hlavné menu | `Forms/MainForm` |
| Výber / vytvorenie / mazanie databázy | `Forms/DatabasePickerForm` |
| Zoznam materiálov (CRUD, hľadanie) | `Forms/MaterialsForm` |
| Zoznam réžií (CRUD, hľadanie) | `Forms/OperationsForm` |
| Kde sa materiál / réžia používa | `Forms/UsageForm` |
| Zoznam výrobkov (filter typu, hľadanie) | `Forms/ProductsForm` |
| Kopírovanie výrobku | `Forms/CloneProductForm` |
| Detail výrobku + zloženie + ceny | `Forms/ProductDetailForm` |
| Riadok zloženia — materiál (hľadanie + zoznam) | `Forms/MaterialRowForm` |
| Riadok zloženia — réžia (hľadanie + zoznam) | `Forms/OperationRowForm` |
| Cenník: filter, tlač, HTML náhľad, export CSV | `Forms/PriceListForm` |
| Koeficienty cenových vzorcov (v percentách, s nápovedou: texty pod nadpismi, ikona ⓘ / F1 / „?“ v titulku) | `Forms/PricingSettingsForm` |
| Ponuka novej verzie (poznámky, sťahovanie) | `Forms/UpdateForm` |

Verzia a ikona:
- Verzia je `<Version>` v `Kalkulacie.App.csproj`; zobrazuje sa dole v hlavnom menu (`AppInfo.Version`).
  Pri každom vydaní (publish) ju treba zvýšiť.
- Ikona `src/Kalkulacie.App/palma.ico` je ikona exe aj všetkých okien — každý formulár dedí z `Forms/AppForm`.

Vzhľad:
- Jednotný vzhľad je v `Forms/Theme.cs` (farby, zaoblené tlačidlá, tabuľky, zoznamy). `AppForm.OnLoad` ho aplikuje
  na všetky ovládacie prvky okna — formuláre samy farby ani štýl tlačidiel nenastavujú, berú ich z `Theme`.
- Hlavné tlačidlo okna (plná farba) je `AcceptButton`, alebo sa označí cez `Theme.Primary(button)`.
- Veľkosť písma ostáva Segoe UI 9 — rozloženie okien (pevné súradnice) je na ňu počítané.
- Položky hlavného menu sú `Forms/NavButton` (karta s ikonou zo Segoe MDL2 Assets, názvom a popisom).

Konvencie UI:
- Detail výrobku ukladá všetko hneď (riadky, typ, poradie); názov tlačidlom alebo pri zatvorení okna.
- Hľadanie ide cez `TextSearch.Matches` — bez ohľadu na veľkosť písmen a diakritiku.
- Materiál/réžiu použitú v zložení nemožno zmazať (FK `Restrict` + kontrola vo formulári).

## Predchodcovia

- FoxPro originál: dáta v `C:\dev\duri\Cennik\DATA` (DBF tabuľky: VSTUPY, VYROBKY, A_VYR, ZLOZENIE, A_ZLOZ, REZIA);
  zdrojáky (`KALK.PRG`, `SCREENS\*.SPR`) sú v `vstupy\Cennik` — priečinok `vstupy\` je v `.gitignore`
- Prvý prepis pokus (Avalonia, s importnou infraštruktúrou): `C:\dev\duri-modern`

## Čo TU NECHCEME

- Migračné/importné polia: `source_table`, `legacy_*`, `created_at`, `updated_at`
- Destructive schema reset (schéma sa mení len migráciami; používateľské DB sa migrujú pri otvorení)
- Import priamo v aplikácii — import je samostatný projekt `Kalkulacie.Import`

## Spustenie

```
dotnet run --project src/Kalkulacie.App
```

Testovací profil (len Debug, do publikovaného exe sa nedostane): otvorí `data\Test.db` bez výberu DB a prázdnu
ju naplní vzorovými dátami (`src/Kalkulacie.App/TestData.cs`). Čistý stav = zmazať `Test.db`.

```
dotnet run --project src/Kalkulacie.App --launch-profile Test
```

## Import z FoxPro

Vždy vytvára DB od nuly (existujúci cieľový súbor zmaže).

```
dotnet run --project src/Kalkulacie.Import -- --source C:\dev\duri\Cennik\DATA --db <cesta>\data\Cennik.db
```

## Publish

Všetky nastavenia publishu sú v `Kalkulacie.App.csproj` (win-x64, self-contained, single-file, kompresia,
natívne dll a symboly vnútri exe) — príkaz nepotrebuje parametre. Výsledok: v `publish\` je len
`Kalkulacie.App.exe` (~50 MB) a `data\`. Na cieľovom PC sa nič neinštaluje (žiadny .NET runtime).
Natívna `e_sqlite3.dll` sa pri prvom štarte rozbalí do `%TEMP%\.net\Kalkulacie.App\`.

```
dotnet publish src/Kalkulacie.App -c Release -o publish
```

`publish\` je v gite ignorovaný; `publish\data` obsahuje ostré DB — nemazať.

Čo do exe nejde: balík `Microsoft.EntityFrameworkCore.Design` (`Publish="false"` — priťahuje kompilátor Roslyn,
treba ho len pre `dotnet ef`), preklady hlášok knižníc (`SatelliteResourceLanguages=en`).
Trimming sa nepoužíva (WinForms ho nepodporuje).

## Vydanie novej verzie

1. Zvýšiť `<Version>` v `Kalkulacie.App.csproj` a commitnúť.
2. `.\release.ps1 -Notes "- čo je nové"` (bez `-Notes` = správy commitov od minulého tagu;
   `-DryRun` = len publish a súhrn, nič sa nepushne).

Skript overí gh CLI a prihlásenie, čistý strom, vetvu master a že tag neexistuje; vyčistí `publish\`
(okrem `data\`), spraví publish, po potvrdení pushne master, vytvorí tag `vX.Y.Z` a GitHub Release
s `Kalkulacie.App.exe` a `Kalkulacie.App.exe.sha256`. Potrebuje gh CLI (`winget install --id GitHub.cli`,
potom `gh auth login`); token musí mať právo zápisu do Contents repa.

## Aktualizácia v aplikácii

`src/Kalkulacie.App/Update/Updater.cs`, UI v `Forms/MainForm` (panel) a `Forms/UpdateForm`. Vlastný kód, bez knižníc.

- Repo je natvrdo `oprencak/kalkulacie` (verejné — aplikácia nepotrebuje token).
- Po zobrazení hlavného okna sa na pozadí zavolá `GET /repos/oprencak/kalkulacie/releases/latest`
  (drafty a pre-release sa ignorujú). Tag `vX.Y.Z` > `AppInfo.Version` → v hlavnom menu sa ukáže panel
  „Nová verzia … je dostupná“ s tlačidlom Aktualizovať. Bez internetu / pri chybe sa nestane nič.
- Kontrola beží len v publikovanom jednosúborovom exe (nie pri `dotnet run` / z Visual Studia).
- Sťahuje sa len `https://github.com/oprencak/kalkulacie/releases/download/<tag>/Kalkulacie.App.exe`
  (adresa sa skladá z pevného repa a overeného tagu). SHA-256 sa overí voči poľu `digest` z GitHub API,
  prípadne voči súboru `.sha256` z release; nezhoda = súbor sa zmaže, nič sa nemení.
- Výmena: stiahnuť do `Kalkulacie.App.exe.new` → bežiaci exe premenovať na `.old` → `.new` na jeho miesto →
  spustiť ho s `--po-aktualizacii <pid>` (počká na skončenie starého procesu, ktorý drží DB) → starý sa ukončí.
- `.old` ostáva počas prvého behu novej verzie (dá sa vrátiť), zmaže sa pri ďalšom úspešnom štarte.
- Dialóg upozorňuje, že DB sa pri otvorení zmigruje a staršia verzia ju nemusí otvoriť. Návrat späť = starý exe
  (`.old` alebo z GitHub Releases) + obnova DB z `data\zalohy` (záloha vzniká pred migráciou).

## Migrácie (pri zmene entít)

Nástroj `dotnet-ef` je lokálny (`dotnet-tools.json`, verzia zhodná s EF Core); prvýkrát `dotnet tool restore`.

```
dotnet ef migrations add NazovMigracie --project src/Kalkulacie.Core --startup-project src/Kalkulacie.App
```
