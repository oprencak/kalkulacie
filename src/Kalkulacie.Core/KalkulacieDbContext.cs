using System.Text.Json;
using Kalkulacie.Core.Calculations;
using Kalkulacie.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kalkulacie.Core;

public class KalkulacieDbContext : DbContext
{
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Operation> Operations => Set<Operation>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CompositionRow> CompositionRows => Set<CompositionRow>();
    public DbSet<PricingSettings> PricingSettings => Set<PricingSettings>();
    public DbSet<CalculationEdition> CalculationEditions => Set<CalculationEdition>();
    public DbSet<CalculationEditionProduct> CalculationEditionProducts => Set<CalculationEditionProduct>();

    /// <summary>Koeficienty cenových vzorcov tejto databázy (nesledovaná kópia); predvolené, ak riadok chýba.</summary>
    public PricingSettings GetPricingSettings() =>
        PricingSettings.AsNoTracking().FirstOrDefault() ?? new PricingSettings();

    public KalkulacieDbContext(DbContextOptions<KalkulacieDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CalculationEdition>(e =>
        {
            e.Property(x => x.Revision).IsConcurrencyToken();
            e.HasMany(x => x.Products).WithOne().HasForeignKey(x => x.CalculationEditionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CalculationEditionProduct>(e =>
        {
            e.HasKey(x => new { x.CalculationEditionId, x.ProductId });
            e.Property(x => x.Calculation).HasConversion(
                calculation => JsonSerializer.Serialize(calculation, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<SavedCalculation>(json, (JsonSerializerOptions?)null)!);
        });

        modelBuilder.Entity<Material>(e =>
        {
            e.HasIndex(m => m.Name);
            e.Property(m => m.Price).HasColumnType("TEXT");
        });

        modelBuilder.Entity<Operation>(e =>
        {
            e.HasIndex(o => o.Name);
            e.Property(o => o.Rate).HasColumnType("TEXT");
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(p => new { p.Name, p.Mode });
            e.Property(p => p.Mode).HasConversion<string>();
        });

        modelBuilder.Entity<PricingSettings>(e =>
        {
            e.Property(s => s.Id).ValueGeneratedNever();
            e.Property(s => s.AreaMaterialManual).HasColumnType("TEXT");
            e.Property(s => s.AreaMaterialAutomatic).HasColumnType("TEXT");
            e.Property(s => s.PieceMaterial).HasColumnType("TEXT");
            e.Property(s => s.MarginManual).HasColumnType("TEXT");
            e.Property(s => s.MarginAutomatic).HasColumnType("TEXT");

            // Jediný riadok s hodnotami z pôvodného FoxPro
            e.HasData(new PricingSettings());
        });

        modelBuilder.Entity<CompositionRow>(e =>
        {
            e.HasIndex(r => r.ProductId);
            e.Property(r => r.RowType).HasConversion<string>();
            e.Property(r => r.Quantity).HasColumnType("TEXT");
            e.Property(r => r.DimensionA).HasColumnType("TEXT");
            e.Property(r => r.DimensionB).HasColumnType("TEXT");

            e.HasOne(r => r.Product)
                .WithMany(p => p.CompositionRows)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Material)
                .WithMany(m => m.CompositionRows)
                .HasForeignKey(r => r.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Operation)
                .WithMany(o => o.CompositionRows)
                .HasForeignKey(r => r.OperationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
