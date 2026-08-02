using Medshop.Modules.Customers.Domain.Entities;

namespace Medshop.Modules.Sales.Domain.Entities;

public class Sale
{
    public long SaleIdPk { get; set; }
    public Guid Id { get; set; }
    public Guid LoginId { get; set; }
    public long CustomerFk { get; set; }
    public string BillNo { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Customer? Customer { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
