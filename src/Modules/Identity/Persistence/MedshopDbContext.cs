using Medshop.Modules.Categories.Domain.Entities;
using Medshop.Modules.Customers.Domain.Entities;
using Medshop.Modules.Identity.Domain.Entities;
using Medshop.Modules.Products.Domain.Entities;
using Medshop.Modules.Sales.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Medshop.Modules.Identity.Persistence;

public class MedshopDbContext : DbContext
{
    public MedshopDbContext(DbContextOptions<MedshopDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

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
            entity.Property(e => e.PurchasePrice).HasColumnType("numeric(18,2)");
            entity.Property(e => e.SellingPrice).HasColumnType("numeric(18,2)");
            entity.Property(e => e.StockQuantity).IsRequired();
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.BatchNo).HasColumnName("batch_no").HasMaxLength(100);
            entity.Property(e => e.Manufacturer).HasColumnName("manufacturer").HasMaxLength(200);
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.HasIndex(e => e.Id).IsUnique();
            entity.HasIndex(e => e.LoginId);
            entity.HasIndex(e => e.Category);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryIdPk);
            entity.Property(e => e.CategoryIdPk).HasColumnName("category_id_pk").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.HasIndex(e => e.Id).IsUnique();
            entity.HasIndex(e => e.LoginId);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(e => e.CustomerIdPk);
            entity.Property(e => e.CustomerIdPk).HasColumnName("customer_pk").ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Mobile).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.HasIndex(e => new { e.LoginId, e.Mobile }).IsUnique();
            entity.HasIndex(e => e.Id).IsUnique();
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("sales");
            entity.HasKey(e => e.SaleIdPk);
            entity.Property(e => e.SaleIdPk).HasColumnName("sale_pk").ValueGeneratedOnAdd();
            entity.Property(e => e.CustomerFk).HasColumnName("customer_fk");
            entity.Property(e => e.BillNo).HasColumnName("bill_no").IsRequired().HasMaxLength(20);
            entity.Property(e => e.Subtotal).HasColumnName("subtotal").HasColumnType("numeric(18,2)");
            entity.Property(e => e.Discount).HasColumnName("discount").HasColumnType("numeric(18,2)");
            entity.Property(e => e.Tax).HasColumnName("tax").HasColumnType("numeric(18,2)");
            entity.Property(e => e.GrandTotal).HasColumnName("grand_total").HasColumnType("numeric(18,2)");
            entity.Property(e => e.PaymentMode).HasColumnName("payment_mode").IsRequired().HasMaxLength(50);
            entity.Property(e => e.BillDate).HasColumnName("bill_date");

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerFk)
                .HasPrincipalKey(c => c.CustomerIdPk)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.LoginId, e.BillNo }).IsUnique();
            entity.HasIndex(e => e.BillDate);
            entity.HasIndex(e => e.CustomerFk);
            entity.HasIndex(e => e.Id).IsUnique();
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.ToTable("sale_items");
            entity.HasKey(e => e.SaleItemIdPk);
            entity.Property(e => e.SaleItemIdPk).HasColumnName("sale_item_pk").ValueGeneratedOnAdd();
            entity.Property(e => e.SaleFk).HasColumnName("sale_fk");
            entity.Property(e => e.ProductFk).HasColumnName("product_fk");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Price).HasColumnName("price").HasColumnType("numeric(18,2)");
            entity.Property(e => e.PurchasePrice).HasColumnName("purchase_price").HasColumnType("numeric(18,2)");
            entity.Property(e => e.Total).HasColumnName("total").HasColumnType("numeric(18,2)");

            entity.HasOne(e => e.Sale)
                .WithMany(s => s.Items)
                .HasForeignKey(e => e.SaleFk)
                .HasPrincipalKey(s => s.SaleIdPk)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductFk)
                .HasPrincipalKey(p => p.ProductIdPk)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.SaleFk);
            entity.HasIndex(e => e.ProductFk);
        });

        base.OnModelCreating(modelBuilder);
    }
}
