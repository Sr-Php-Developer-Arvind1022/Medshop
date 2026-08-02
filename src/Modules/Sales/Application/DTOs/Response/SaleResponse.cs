namespace Medshop.Modules.Sales.Application.DTOs.Response;

public class SaleResponse
{
    public long SalePk { get; set; }
    public Guid Id { get; set; }
    public string BillNo { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public SaleCustomerResponse Customer { get; set; } = new();
    public IReadOnlyCollection<SaleItemResponse> Items { get; set; } = Array.Empty<SaleItemResponse>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class SaleCustomerResponse
{
    public long CustomerPk { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Address { get; set; }
}

public class SaleItemResponse
{
    public long SaleItemPk { get; set; }
    public long ProductFk { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal Total { get; set; }
    public decimal Profit { get; set; }
}
