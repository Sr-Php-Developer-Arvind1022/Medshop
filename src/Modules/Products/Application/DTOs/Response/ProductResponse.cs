namespace Medshop.Modules.Products.Application.DTOs.Response;

public class ProductResponse
{
    public Guid Id { get; set; }
    public Guid LoginId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ProductImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}