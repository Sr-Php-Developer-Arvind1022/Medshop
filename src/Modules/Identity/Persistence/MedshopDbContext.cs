using Medshop.Modules.Identity.Domain.Entities;
using Medshop.Modules.Products.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Medshop.Modules.Identity.Persistence;

public class MedshopDbContext : DbContext
{
    public MedshopDbContext(DbContextOptions<MedshopDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserIdPk);
            entity.Property(e => e.UserIdPk).HasColumnName("user_id_pk").ValueGeneratedOnAdd();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Mobile).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.IsActive);
            entity.HasIndex(e => e.Id);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductIdPk);
            entity.Property(e => e.ProductIdPk).HasColumnName("product_id_pk").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("numeric(18,2)");
            entity.Property(e => e.StockQuantity).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.HasIndex(e => e.Id).IsUnique();
            entity.HasIndex(e => e.LoginId);
            entity.HasIndex(e => e.Category);
        });

        base.OnModelCreating(modelBuilder);
    }
}
