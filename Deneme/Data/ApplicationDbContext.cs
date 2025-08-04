using Microsoft.EntityFrameworkCore;
using Deneme.Models;

namespace Deneme.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product yapılandırması
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Title).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Description).HasMaxLength(1000);
                entity.Property(p => p.StockQuantity).IsRequired();
                
                // İlişki yapılandırması
                entity.HasOne(p => p.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                // İndeksler
                entity.HasIndex(p => p.Title);
                entity.HasIndex(p => p.CategoryId);
                entity.HasIndex(p => p.StockQuantity);
            });

            // Category yapılandırması
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Description).HasMaxLength(500);
                entity.Property(c => c.MinimumStockQuantity).IsRequired();

                // İndeks
                entity.HasIndex(c => c.Name);
            });

            // Seed data
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Elektronik", Description = "Elektronik ürünler", MinimumStockQuantity = 5 },
                new Category { Id = 2, Name = "Giyim", Description = "Giyim ürünleri", MinimumStockQuantity = 10 },
                new Category { Id = 3, Name = "Kitap", Description = "Kitaplar ve dergiler", MinimumStockQuantity = 3 }
            );
        }
    }
}