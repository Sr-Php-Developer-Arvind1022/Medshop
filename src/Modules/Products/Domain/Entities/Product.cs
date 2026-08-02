namespace Medshop.Modules.Products.Domain.Entities;

public class Product
{
    public long ProductIdPk { get; set; }
    public Guid Id { get; set; }
    public Guid LoginId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int StockQuantity { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? BatchNo { get; set; }
    public string? Manufacturer { get; set; }
    public string? ProductImage { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}