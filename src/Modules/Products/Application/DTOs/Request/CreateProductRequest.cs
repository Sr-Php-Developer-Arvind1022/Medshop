using Microsoft.AspNetCore.Http;

namespace Medshop.Modules.Products.Application.DTOs.Request;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int StockQuantity { get; set; }
    public IFormFile? ProductImage { get; set; }
}