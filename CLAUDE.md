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

## Databázové entity (čisté, bez legacy polí)

```
Material    Id, Name, Unit?, Price
Operation   Id, Name, Rate
Product     Id, Name, Mode (Manual/Automatic)
CompositionRow  Id, ProductId, RowType, MaterialId?, DimensionA?, DimensionB?,
                OperationId?, Quantity
```

Migrácie: `src/Kalkulacie.Core/Migrations/`

## Cenové vzorce (z FoxPro KALK.PRG)

```
Materiál — plošný (DimensionA != 0 && DimensionB != 0):
  Manual:     cost = A × B × 1.15 × Price × Qty
  Automatic:  cost = A × B × 1.05 × Price × Qty

Materiál — kusový:
  cost = Price × Qty × 1.03

Réžia/práca:
  cost = Rate / Qty

Súhrn:
  UVN = sum(material_costs) + sum(operation_costs)
  Manual:     BasePrice = UVN × 1.15
  Automatic:  BasePrice = UVN × 1.08
  Price1 = BasePrice × 1.08
  Price2 = BasePrice × 1.16
```

Implementácia: `src/Kalkulacie.Core/Calculations/PriceCalculator.cs`

## WinForms formuláre

| Formulár | Súbor |
|---|---|
| Hlavné menu | `Forms/MainForm` |
| Zoznam materiálov (CRUD) | `Forms/MaterialsForm` |
| Zoznam réžií (CRUD) | `Forms/OperationsForm` |
| Zoznam výrobkov | `Forms/ProductsForm` |
| Detail výrobku + zloženie + ceny | `Forms/ProductDetailForm` |
| Výber materiálu (picker) | `Forms/MaterialPickerForm` |
| Výber réžie (picker) | `Forms/OperationPickerForm` |

## Predchodcovia

- FoxPro originál: `C:\dev\duri` (DBF tabuľky: VSTUPY, VYROBKY, A_VYR, ZLOZENIE, A_ZLOZ, REZIA)
- Prvý prepis pokus (Avalonia, s importnou infraštruktúrou): `C:\dev\duri-modern`

## Čo TU NECHCEME

- Migračné/importné polia: `source_table`, `legacy_*`, `created_at`, `updated_at`
- Destructive schema reset
- Importný modul (rieši sa zvlášť neskôr)

## Spustenie

```
dotnet run --project src/Kalkulacie.App
```

## Migrácie (pri zmene entít)

```
dotnet ef migrations add NazovMigracie --project src/Kalkulacie.Core --startup-project src/Kalkulacie.App
```
