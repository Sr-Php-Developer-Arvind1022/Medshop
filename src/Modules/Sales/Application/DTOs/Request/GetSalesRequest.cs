namespace Medshop.Modules.Sales.Application.DTOs.Request;

public class GetSalesRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? PaymentMode { get; set; }
    public string? CustomerMobile { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
