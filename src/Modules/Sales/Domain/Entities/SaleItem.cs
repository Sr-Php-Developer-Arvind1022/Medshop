using Medshop.Modules.Products.Domain.Entities;

namespace Medshop.Modules.Sales.Domain.Entities;

public class SaleItem
{
    public long SaleItemIdPk { get; set; }
    public long SaleFk { get; set; }
    public long ProductFk { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Sale? Sale { get; set; }
    public Product? Product { get; set; }
}
