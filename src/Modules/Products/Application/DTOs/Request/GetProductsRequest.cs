namespace Medshop.Modules.Products.Application.DTOs.Request;

public class GetProductsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Category { get; set; }
    public decimal? MinPurchasePrice { get; set; }
    public decimal? MaxPurchasePrice { get; set; }
    public decimal? MinSellingPrice { get; set; }
    public decimal? MaxSellingPrice { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public Guid? LoginId { get; set; }
}