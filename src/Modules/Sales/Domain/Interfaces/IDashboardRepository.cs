namespace Medshop.Modules.Sales.Domain.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardCardsReadModel> GetCardsAsync(Guid loginId, DateTime todayStart, DateTime todayEnd, int lowStockThreshold, CancellationToken cancellationToken);
    Task<DashboardStockSummaryReadModel> GetStockSummaryAsync(Guid loginId, DateTime rangeStart, DateTime rangeEnd, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardLowStockProductReadModel>> GetLowStockProductsAsync(Guid loginId, int lowStockThreshold, int take, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardOutOfStockProductReadModel>> GetOutOfStockProductsAsync(Guid loginId, int take, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardExpiryProductReadModel>> GetExpiryProductsAsync(Guid loginId, DateTime today, DateTime expiryToDate, int take, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardGraphPointReadModel>> GetSalesGraphAsync(Guid loginId, DateTime startDate, DateTime endDateExclusive, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardGraphPointReadModel>> GetPurchaseGraphAsync(Guid loginId, DateTime startDate, DateTime endDateExclusive, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardTopSellingProductReadModel>> GetTopSellingProductsAsync(Guid loginId, DateTime rangeStart, DateTime rangeEnd, int take, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DashboardRecentSaleReadModel>> GetRecentSalesAsync(Guid loginId, int take, CancellationToken cancellationToken);
    Task<DashboardStatisticsReadModel> GetStatisticsAsync(Guid loginId, DateTime todayStart, DateTime todayEnd, DateTime yesterdayStart, DateTime yesterdayEnd, DateTime rangeStart, DateTime rangeEnd, CancellationToken cancellationToken);
    Task<DashboardStockValueSummaryReadModel> GetStockValueSummaryAsync(Guid loginId, CancellationToken cancellationToken);
}

public class DashboardCardsReadModel
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

public class DashboardStockSummaryReadModel
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

public class DashboardLowStockProductReadModel
{
    public long ProductPk { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public decimal SellingPrice { get; set; }
}

public class DashboardOutOfStockProductReadModel
{
    public long ProductPk { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public DateTime? LastPurchaseDate { get; set; }
    public DateTime? LastSaleDate { get; set; }
}

public class DashboardExpiryProductReadModel
{
    public string ProductName { get; set; } = string.Empty;
    public string? BatchNo { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int Stock { get; set; }
}

public class DashboardGraphPointReadModel
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public class DashboardTopSellingProductReadModel
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Amount { get; set; }
}

public class DashboardRecentSaleReadModel
{
    public string BillNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int ItemsCount { get; set; }
    public decimal GrandTotal { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
}

public class DashboardStatisticsReadModel
{
    public int TodayOrders { get; set; }
    public int YesterdayOrders { get; set; }
    public decimal AverageBillValue { get; set; }
    public decimal AverageProfitPerBill { get; set; }
    public int RepeatCustomers { get; set; }
    public int NewCustomers { get; set; }
}

public class DashboardStockValueSummaryReadModel
{
    public decimal PurchaseStockValue { get; set; }
    public decimal SellingStockValue { get; set; }
    public decimal ExpectedProfit { get; set; }
}
