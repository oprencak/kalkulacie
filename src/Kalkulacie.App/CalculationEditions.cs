using Kalkulacie.Core.Calculations;
using Kalkulacie.Core.Entities;
using Kalkulacie.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kalkulacie.App;

internal static class CalculationEditions
{
    internal sealed record ListItem(int Id, int Revision, string Name, DateOnly Date, List<string> ProductNames);
    internal sealed record ProductChoice(int Id, string Name, CalculationMode Mode);

    public static List<ListItem> List()
    {
        using var db = AppDb.CreateContext();
        return db.CalculationEditions.AsNoTracking()
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
            .Select(x => new ListItem(x.Id, x.Revision, x.Name, x.Date,
                x.Products.Select(p => p.ProductName).ToList()))
            .ToList();
    }

    public static CalculationEdition Open(int id)
    {
        using var db = AppDb.CreateContext();
        return db.CalculationEditions.AsNoTracking().Include(x => x.Products)
            .SingleOrDefault(x => x.Id == id) ?? throw ChangedEdition();
    }

    public static List<ProductChoice> Catalog()
    {
        using var db = AppDb.CreateContext();
        return db.Products.Select(p => new ProductChoice(p.Id, p.Name, p.Mode)).AsEnumerable()
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(p => p.Mode).ToList();
    }

    public static List<CalculationEditionProduct> Calculate(IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToArray();
        if (ids.Length == 0) return [];

        using var db = AppDb.CreateContext();
        using var transaction = db.Database.BeginTransaction();
        var settings = db.GetPricingSettings();
        var products = db.Products.Where(p => ids.Contains(p.Id))
            .Include(p => p.CompositionRows).ThenInclude(r => r.Material)
            .Include(p => p.CompositionRows).ThenInclude(r => r.Operation)
            .AsNoTrackingWithIdentityResolution().ToList();
        transaction.Commit();

        var calculatedAt = DateTime.UtcNow;
        return products.Select(p => new CalculationEditionProduct
        {
            ProductId = p.Id,
            ProductName = p.Name,
            Calculation = SavedCalculation.Capture(p, settings, calculatedAt),
        }).ToList();
    }

    public static void Save(CalculationEdition edition)
    {
        var name = edition.Name.Trim();
        if (name.Length == 0) throw new InvalidOperationException("Zadajte názov kalkulácie.");

        using var db = AppDb.CreateContext();
        var saved = edition.Id == 0 ? new CalculationEdition() : db.CalculationEditions.Include(x => x.Products)
            .SingleOrDefault(x => x.Id == edition.Id && x.Revision == edition.Revision) ?? throw ChangedEdition();

        saved.Name = name;
        saved.Date = edition.Date;
        saved.Note = edition.Note;
        saved.Revision++;

        var incoming = edition.Products.ToDictionary(x => x.ProductId);
        foreach (var product in saved.Products.ToArray())
        {
            if (incoming.Remove(product.ProductId, out var updated))
            {
                product.ProductName = updated.ProductName;
                product.Calculation = updated.Calculation;
            }
            else
            {
                db.CalculationEditionProducts.Remove(product);
            }
        }
        foreach (var product in incoming.Values)
        {
            saved.Products.Add(new CalculationEditionProduct
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Calculation = product.Calculation,
            });
        }

        if (edition.Id == 0) db.CalculationEditions.Add(saved);

        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw ChangedEdition();
        }
        edition.Id = saved.Id;
        edition.Revision = saved.Revision;
        edition.Name = saved.Name;
    }

    public static void Delete(ListItem edition)
    {
        using var db = AppDb.CreateContext();
        if (db.CalculationEditions.Where(x => x.Id == edition.Id && x.Revision == edition.Revision).ExecuteDelete() == 0)
            throw ChangedEdition();
    }

    private static InvalidOperationException ChangedEdition() =>
        new("Kalkulácia bola medzitým zmenená alebo zmazaná. Otvorte ju znova zo zoznamu.");
}
