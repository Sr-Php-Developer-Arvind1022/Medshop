namespace Medshop.Modules.Sales.Application.DTOs.Response;

public class DashboardResponse
{
    public decimal TodayPurchaseAmount { get; set; }
    public decimal TodaySaleAmount { get; set; }
    public decimal TodayProfit { get; set; }
    public decimal CurrentStockPurchaseValue { get; set; }
    public decimal CurrentStockSellingValue { get; set; }
    public decimal ExpectedProfit { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int ExpiryWithin30Days { get; set; }
    public IReadOnlyCollection<TopSellingProductResponse> TopSellingProducts { get; set; } = Array.Empty<TopSellingProductResponse>();
    public IReadOnlyCollection<SaleResponse> RecentSales { get; set; } = Array.Empty<SaleResponse>();
    public IReadOnlyCollection<DashboardProductResponse> LowStockProducts { get; set; } = Array.Empty<DashboardProductResponse>();
    public IReadOnlyCollection<DashboardProductResponse> OutOfStockProducts { get; set; } = Array.Empty<DashboardProductResponse>();
}

public class TopSellingProductResponse
{
    public long ProductFk { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalSaleAmount { get; set; }
}

public class DashboardProductResponse
{
    public long ProductPk { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
