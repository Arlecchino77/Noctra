using Microsoft.EntityFrameworkCore;
using Noctra.Models;

namespace Noctra.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Filter> Filters => Set<Filter>();
    public DbSet<FilterOption> FilterOptions => Set<FilterOption>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
        b.Entity<Collection>().HasIndex(c => c.Slug).IsUnique();
        b.Entity<Filter>().HasIndex(f => f.Code).IsUnique();

        // SQLite не умеет сравнивать decimal, поэтому цена хранится как REAL
        b.Entity<Product>().Property(p => p.Price).HasConversion<double>();

        b.Entity<Product>()
            .HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Product>()
            .HasOne(p => p.Collection).WithMany(c => c.Products)
            .HasForeignKey(p => p.CollectionId).OnDelete(DeleteBehavior.SetNull);

        // Связь товаров со значениями фильтров (многие ко многим)
        b.Entity<Product>()
            .HasMany(p => p.FilterOptions).WithMany(o => o.Products)
            .UsingEntity(j => j.ToTable("ProductFilterOptions"));

        b.Entity<CartItem>()
            .HasIndex(c => new { c.UserId, c.ProductId, c.Size }).IsUnique();
    }
}
