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

    /// <summary>Koeficienty cenových vzorcov tejto databázy (nesledovaná kópia); predvolené, ak riadok chýba.</summary>
    public PricingSettings GetPricingSettings() =>
        PricingSettings.AsNoTracking().FirstOrDefault() ?? new PricingSettings();

    public KalkulacieDbContext(DbContextOptions<KalkulacieDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
