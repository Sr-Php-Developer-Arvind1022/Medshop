using Microsoft.AspNetCore.Http;

namespace Medshop.Modules.Products.Application.DTOs.Request;

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public IFormFile? ProductImage { get; set; }
}