namespace Medshop.Modules.Sales.Application.DTOs.Response;

public class DashboardDto
{
    public DashboardCardsDto Cards { get; set; } = new();
    public DashboardStockSummaryDto StockSummary { get; set; } = new();
    public IReadOnlyCollection<DashboardLowStockProductDto> LowStockProducts { get; set; } = Array.Empty<DashboardLowStockProductDto>();
    public IReadOnlyCollection<DashboardOutOfStockProductDto> OutOfStockProducts { get; set; } = Array.Empty<DashboardOutOfStockProductDto>();
    public IReadOnlyCollection<DashboardExpiryProductDto> ExpiryProducts { get; set; } = Array.Empty<DashboardExpiryProductDto>();
    public IReadOnlyCollection<DashboardSalesGraphPointDto> SalesGraph { get; set; } = Array.Empty<DashboardSalesGraphPointDto>();
    public IReadOnlyCollection<DashboardPurchaseGraphPointDto> PurchaseGraph { get; set; } = Array.Empty<DashboardPurchaseGraphPointDto>();
    public IReadOnlyCollection<DashboardTopSellingProductDto> TopSellingProducts { get; set; } = Array.Empty<DashboardTopSellingProductDto>();
    public IReadOnlyCollection<DashboardRecentSaleDto> RecentSales { get; set; } = Array.Empty<DashboardRecentSaleDto>();
    public DashboardStatisticsDto Statistics { get; set; } = new();
    public DashboardStockValueSummaryDto StockValueSummary { get; set; } = new();
}

public class DashboardCardsDto
{
    public decimal TodayPurchaseAmount { get; set; }
    public decimal TodaySalesAmount { get; set; }
    public decimal TodayProfit { get; set; }
    public decimal CurrentStockPurchaseValue { get; set; }
    public decimal CurrentStockSellingValue { get; set; }
    public decimal ExpectedProfit { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int ExpiryWithin30DaysCount { get; set; }
}

public class DashboardStockSummaryDto
{
    public decimal PurchaseAmount { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal Profit { get; set; }
    public int PurchasedQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public int PurchaseTransactions { get; set; }
    public int SalesTransactions { get; set; }
    public decimal AverageProfitPercentage { get; set; }
}

public class DashboardLowStockProductDto
{
    public long ProductPk { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public decimal SellingPrice { get; set; }
}

public class DashboardOutOfStockProductDto
{
    public long ProductPk { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public DateTime? LastPurchaseDate { get; set; }
    public DateTime? LastSaleDate { get; set; }
}

public class DashboardExpiryProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public string? BatchNo { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int Stock { get; set; }
}

public class DashboardSalesGraphPointDto
{
    public DateTime Date { get; set; }
    public decimal Sales { get; set; }
}

public class DashboardPurchaseGraphPointDto
{
    public DateTime Date { get; set; }
    public decimal PurchaseAmount { get; set; }
}

public class DashboardTopSellingProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Amount { get; set; }
}

public class DashboardRecentSaleDto
{
    public string BillNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int ItemsCount { get; set; }
    public decimal GrandTotal { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
}

public class DashboardStatisticsDto
{
    public int TodayOrders { get; set; }
    public int YesterdayOrders { get; set; }
    public decimal AverageBillValue { get; set; }
    public decimal AverageProfitPerBill { get; set; }
    public int RepeatCustomers { get; set; }
    public int NewCustomers { get; set; }
}

public class DashboardStockValueSummaryDto
{
    public decimal PurchaseStockValue { get; set; }
    public decimal SellingStockValue { get; set; }
    public decimal ExpectedProfit { get; set; }
}
