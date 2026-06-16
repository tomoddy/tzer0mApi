using Microsoft.EntityFrameworkCore;
using tzer0mApi.Models.StockWise;

namespace tzer0mApi.Services.StockWise;

/// <summary>
/// EF Core database context for the StockWise application.
/// </summary>
/// <remarks>
/// Initialises a new instance of the StockWise database context.
/// </remarks>
public class StockWiseDbContext(DbContextOptions<StockWiseDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The storage categories table.
    /// </summary>
    public DbSet<StorageCategory> StorageCategories => Set<StorageCategory>();

    /// <summary>
    /// The locations table.
    /// </summary>
    public DbSet<Location> Locations => Set<Location>();

    /// <summary>
    /// The items table.
    /// </summary>
    public DbSet<Item> Items => Set<Item>();

    /// <summary>
    /// The item storage category rules table.
    /// </summary>
    public DbSet<ItemStorageCategory> ItemStorageCategories => Set<ItemStorageCategory>();

    /// <summary>
    /// The stock table.
    /// </summary>
    public DbSet<Stock> Stock => Set<Stock>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Composite primary key for ItemStorageCategories
        modelBuilder.Entity<ItemStorageCategory>()
            .HasKey(x => new { x.ItemId, x.CategoryId });

        // Item -> ItemStorageCategories
        modelBuilder.Entity<ItemStorageCategory>()
            .HasOne(x => x.Item)
            .WithMany(x => x.ItemStorageCategories)
            .HasForeignKey(x => x.ItemId);

        // StorageCategory -> ItemStorageCategories
        modelBuilder.Entity<ItemStorageCategory>()
            .HasOne(x => x.StorageCategory)
            .WithMany(x => x.ItemStorageCategories)
            .HasForeignKey(x => x.CategoryId);

        // StorageCategory -> Locations
        modelBuilder.Entity<Location>()
            .HasOne(x => x.StorageCategory)
            .WithMany(x => x.Locations)
            .HasForeignKey(x => x.CategoryId);

        // Item -> Stock
        modelBuilder.Entity<Stock>()
            .HasOne(x => x.Item)
            .WithMany(x => x.Stock)
            .HasForeignKey(x => x.ItemId);

        // Location -> Stock
        modelBuilder.Entity<Stock>()
            .HasOne(x => x.Location)
            .WithMany(x => x.Stock)
            .HasForeignKey(x => x.LocationId);

        // Map to public schema with quoted PascalCase table names
        modelBuilder.Entity<StorageCategory>().ToTable("StorageCategories");
        modelBuilder.Entity<Location>().ToTable("Locations");
        modelBuilder.Entity<Item>().ToTable("Items");
        modelBuilder.Entity<ItemStorageCategory>().ToTable("ItemStorageCategories");
        modelBuilder.Entity<Stock>().ToTable("Stock");
    }
}