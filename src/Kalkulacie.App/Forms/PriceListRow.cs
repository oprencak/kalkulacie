namespace Kalkulacie.App.Forms;

public record PriceListRow(
    string Výrobok,
    string Typ,
    decimal Materiál,
    decimal Práca,
    decimal UVN,
    decimal Cena
);
