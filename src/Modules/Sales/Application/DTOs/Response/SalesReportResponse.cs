namespace Medshop.Modules.Sales.Application.DTOs.Response;

public class SalesReportResponse
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalBills { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public IReadOnlyCollection<SaleResponse> Sales { get; set; } = Array.Empty<SaleResponse>();
}
