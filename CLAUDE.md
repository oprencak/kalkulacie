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
| UI | WinForms (.NET 8) |
| Databáza | SQLite — `*.db` v priečinku `data\` vedľa `Kalkulacie.App.exe` (viac DB → výber pri štarte, zmena v hlavnom menu; `AppDb.cs`) |
| ORM | EF Core 8 (code-first, inkrementálne migrácie) |

Projekty:
- `src/Kalkulacie.Core` — entity, DbContext, cenové vzorce
- `src/Kalkulacie.App` — WinForms formuláre
- `src/Kalkulacie.Import` — konzolový import z FoxPro DBF do novej SQLite DB (vlastný DBF reader v `Dbf/`)

Zálohy: pri každom otvorení DB sa pred migráciou uloží kópia do `data\zalohy\<db>_<yyyyMMdd_HHmmss>.db`
(len ak sa DB od poslednej zálohy zmenila; drží sa posledných 10 na databázu) — `AppDb.Backup`.

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
| Koeficienty cenových vzorcov (v percentách) | `Forms/PricingSettingsForm` |

Verzia a ikona:
- Verzia je `<Version>` v `Kalkulacie.App.csproj`; zobrazuje sa dole v hlavnom menu (`AppInfo.Version`).
  Pri každom vydaní (publish) ju treba zvýšiť.
- Ikona `src/Kalkulacie.App/palma.ico` je ikona exe aj všetkých okien — každý formulár dedí z `Forms/AppForm`.

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

## Import z FoxPro

Vždy vytvára DB od nuly (existujúci cieľový súbor zmaže).

```
dotnet run --project src/Kalkulacie.Import -- --source C:\dev\duri\Cennik\DATA --db <cesta>\data\Cennik.db
```

## Publish

Samostatný jednosúborový exe do `publish\` (v gite ignorovaný; `publish\data` obsahuje ostré DB — nemazať):

```
dotnet publish src/Kalkulacie.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

## Migrácie (pri zmene entít)

```
dotnet ef migrations add NazovMigracie --project src/Kalkulacie.Core --startup-project src/Kalkulacie.App
```
