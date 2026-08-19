using Microsoft.EntityFrameworkCore;
using TechnicalChallenge.API.Entities;

namespace TechnicalChallenge.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Extraction> Extractions { get; set; }
    public DbSet<ExtractionItem> ExtractionItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                .ValueGeneratedOnAdd();

            // ExternalId debe ser único (identidad en la fuente externa)
            entity.HasIndex(p => p.ExternalId)
                .IsUnique();

            // Configuración de precisión para Price
            entity.Property(p => p.Price)
                .HasPrecision(18, 2);

            // SourceUrl no puede ser null
            entity.Property(p => p.SourceUrl)
                .IsRequired();
        });

        // Configuración de Extraction
        modelBuilder.Entity<Extraction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // Convertir enum a string en la BD
            entity.Property(e => e.Status)
                .HasConversion<string>();

            // Relación: Extraction tiene muchos ExtractionItems
            entity.HasMany(e => e.ExtractionItems)
                .WithOne(ei => ei.Extraction)
                .HasForeignKey(ei => ei.ExtractionId)
                .OnDelete(DeleteBehavior.Cascade); // Si borramos Extraction, borramos sus Items
        });

        // Configuración de ExtractionItem
        modelBuilder.Entity<ExtractionItem>(entity =>
        {
            entity.HasKey(ei => ei.Id);

            entity.Property(ei => ei.Id)
                .ValueGeneratedOnAdd();

            // Convertir enum a string
            entity.Property(ei => ei.Status)
                .HasConversion<string>();

            // Relación con Product: Restrict para no borrar productos que tengan historial
            entity.HasOne(ei => ei.Product)
                .WithMany(p => p.ExtractionItems)
                .HasForeignKey(ei => ei.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}