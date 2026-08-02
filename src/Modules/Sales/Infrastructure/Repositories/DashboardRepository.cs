using Medshop.Modules.Identity.Persistence;
using Medshop.Modules.Sales.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Medshop.Modules.Sales.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly MedshopDbContext _context;

    public DashboardRepository(MedshopDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardCardsReadModel> GetCardsAsync(Guid loginId, DateTime todayStart, DateTime todayEnd, int lowStockThreshold, CancellationToken cancellationToken)
    {
        var todayMetrics = await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale != null && i.Sale.LoginId == loginId && i.Sale.BillDate >= todayStart && i.Sale.BillDate < todayEnd)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TodayPurchaseAmount = g.Sum(x => x.PurchasePrice * x.Quantity),
                TodaySalesAmount = g.Sum(x => x.Total)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;
        var next30Days = today.AddDays(30);

        var stockMetrics = await _context.Products
            .AsNoTracking()
            .Where(p => p.LoginId == loginId && !p.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                CurrentStockPurchaseValue = g.Sum(x => x.PurchasePrice * x.StockQuantity),
                CurrentStockSellingValue = g.Sum(x => x.SellingPrice * x.StockQuantity),
                TotalProducts = g.Count(),
                LowStockCount = g.Count(x => x.StockQuantity > 0 && x.StockQuantity <= lowStockThreshold),
                OutOfStockCount = g.Count(x => x.StockQuantity == 0),
                ExpiryWithin30DaysCount = g.Count(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value.Date >= today && x.ExpiryDate.Value.Date <= next30Days)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var todayPurchase = todayMetrics?.TodayPurchaseAmount ?? 0m;
        var todaySales = todayMetrics?.TodaySalesAmount ?? 0m;
        var stockPurchase = stockMetrics?.CurrentStockPurchaseValue ?? 0m;
        var stockSelling = stockMetrics?.CurrentStockSellingValue ?? 0m;

        return new DashboardCardsReadModel
        {
            TodayPurchaseAmount = todayPurchase,
            TodaySalesAmount = todaySales,
            TodayProfit = todaySales - todayPurchase,
            CurrentStockPurchaseValue = stockPurchase,
            CurrentStockSellingValue = stockSelling,
            ExpectedProfit = stockSelling - stockPurchase,
            TotalProducts = stockMetrics?.TotalProducts ?? 0,
            LowStockCount = stockMetrics?.LowStockCount ?? 0,
            OutOfStockCount = stockMetrics?.OutOfStockCount ?? 0,
            ExpiryWithin30DaysCount = stockMetrics?.ExpiryWithin30DaysCount ?? 0
        };
    }

    public async Task<DashboardStockSummaryReadModel> GetStockSummaryAsync(Guid loginId, DateTime rangeStart, DateTime rangeEnd, CancellationToken cancellationToken)
    {
        var summary = await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale != null && i.Sale.LoginId == loginId && i.Sale.BillDate >= rangeStart && i.Sale.BillDate < rangeEnd)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                PurchaseAmount = g.Sum(x => x.PurchasePrice * x.Quantity),
                SalesAmount = g.Sum(x => x.Total),
                Quantity = g.Sum(x => x.Quantity)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var transactions = await _context.Sales
            .AsNoTracking()
            .Where(s => s.LoginId == loginId && s.BillDate >= rangeStart && s.BillDate < rangeEnd)
            .CountAsync(cancellationToken);

        var purchaseAmount = summary?.PurchaseAmount ?? 0m;
        var salesAmount = summary?.SalesAmount ?? 0m;
        var profit = salesAmount - purchaseAmount;

        return new DashboardStockSummaryReadModel
        {
            PurchaseAmount = purchaseAmount,
            SalesAmount = salesAmount,
            Profit = profit,
            PurchasedQuantity = summary?.Quantity ?? 0,
            SoldQuantity = summary?.Quantity ?? 0,
            PurchaseTransactions = transactions,
            SalesTransactions = transactions,
            AverageProfitPercentage = purchaseAmount <= 0 ? 0 : decimal.Round((profit / purchaseAmount) * 100m, 2)
        };
    }

    public async Task<IReadOnlyCollection<DashboardLowStockProductReadModel>> GetLowStockProductsAsync(Guid loginId, int lowStockThreshold, int take, CancellationToken cancellationToken)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.LoginId == loginId && !p.IsDeleted && p.StockQuantity > 0 && p.StockQuantity <= lowStockThreshold)
            .OrderBy(p => p.StockQuantity)
            .ThenBy(p => p.Name)
            .Take(take)
            .Select(p => new DashboardLowStockProductReadModel
            {
                ProductPk = p.ProductIdPk,
                ProductName = p.Name,
                Stock = p.StockQuantity,
                MinimumStock = lowStockThreshold,
                SellingPrice = p.SellingPrice
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DashboardOutOfStockProductReadModel>> GetOutOfStockProductsAsync(Guid loginId, int take, CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.LoginId == loginId && !p.IsDeleted && p.StockQuantity == 0)
            .Select(p => new DashboardOutOfStockProductReadModel
            {
                ProductPk = p.ProductIdPk,
                ProductName = p.Name,
                LastPurchaseDate = p.UpdatedAt,
                LastSaleDate = _context.SaleItems
                    .AsNoTracking()
                    .Where(si => si.ProductFk == p.ProductIdPk && si.Sale != null && si.Sale.LoginId == loginId)
                    .Select(si => (DateTime?)si.Sale!.BillDate)
                    .Max()
            });

        return await query
            .OrderBy(x => x.ProductName)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DashboardExpiryProductReadModel>> GetExpiryProductsAsync(Guid loginId, DateTime today, DateTime expiryToDate, int take, CancellationToken cancellationToken)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.LoginId == loginId && !p.IsDeleted && p.ExpiryDate.HasValue)
            .Where(p => p.ExpiryDate!.Value.Date >= today && p.ExpiryDate.Value.Date <= expiryToDate)
            .OrderBy(p => p.ExpiryDate)
            .ThenBy(p => p.Name)
            .Take(take)
            .Select(p => new DashboardExpiryProductReadModel
            {
                ProductName = p.Name,
                BatchNo = p.BatchNo,
                ExpiryDate = p.ExpiryDate!.Value,
                Stock = p.StockQuantity
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DashboardGraphPointReadModel>> GetSalesGraphAsync(Guid loginId, DateTime startDate, DateTime endDateExclusive, CancellationToken cancellationToken)
    {
        var raw = await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale != null && i.Sale.LoginId == loginId && i.Sale.BillDate >= startDate && i.Sale.BillDate < endDateExclusive)
            .GroupBy(i => i.Sale!.BillDate.Date)
            .Select(g => new DashboardGraphPointReadModel
            {
                Date = g.Key,
                Amount = g.Sum(x => x.Total)
            })
            .ToListAsync(cancellationToken);

        return BuildLast7DaysGraph(raw, endDateExclusive.Date.AddDays(-1));
    }

    public async Task<IReadOnlyCollection<DashboardGraphPointReadModel>> GetPurchaseGraphAsync(Guid loginId, DateTime startDate, DateTime endDateExclusive, CancellationToken cancellationToken)
    {
        var raw = await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale != null && i.Sale.LoginId == loginId && i.Sale.BillDate >= startDate && i.Sale.BillDate < endDateExclusive)
            .GroupBy(i => i.Sale!.BillDate.Date)
            .Select(g => new DashboardGraphPointReadModel
            {
                Date = g.Key,
                Amount = g.Sum(x => x.PurchasePrice * x.Quantity)
            })
            .ToListAsync(cancellationToken);

        return BuildLast7DaysGraph(raw, endDateExclusive.Date.AddDays(-1));
    }

    public async Task<IReadOnlyCollection<DashboardTopSellingProductReadModel>> GetTopSellingProductsAsync(Guid loginId, DateTime rangeStart, DateTime rangeEnd, int take, CancellationToken cancellationToken)
    {
        return await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale != null && i.Sale.LoginId == loginId && i.Sale.BillDate >= rangeStart && i.Sale.BillDate < rangeEnd)
            .GroupBy(i => new { i.ProductFk, ProductName = i.Product != null ? i.Product.Name : string.Empty })
            .Select(g => new DashboardTopSellingProductReadModel
            {
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(x => x.Quantity),
                Amount = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.QuantitySold)
            .ThenByDescending(x => x.Amount)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DashboardRecentSaleReadModel>> GetRecentSalesAsync(Guid loginId, int take, CancellationToken cancellationToken)
    {
        return await _context.Sales
            .AsNoTracking()
            .Where(s => s.LoginId == loginId)
            .OrderByDescending(s => s.BillDate)
            .Take(take)
            .Select(s => new DashboardRecentSaleReadModel
            {
                BillNo = s.BillNo,
                CustomerName = s.Customer != null ? s.Customer.Name : string.Empty,
                ItemsCount = s.Items.Count,
                GrandTotal = s.GrandTotal,
                PaymentMode = s.PaymentMode,
                BillDate = s.BillDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DashboardStatisticsReadModel> GetStatisticsAsync(Guid loginId, DateTime todayStart, DateTime todayEnd, DateTime yesterdayStart, DateTime yesterdayEnd, DateTime rangeStart, DateTime rangeEnd, CancellationToken cancellationToken)
    {
        var todayOrders = await _context.Sales
            .AsNoTracking()
            .CountAsync(s => s.LoginId == loginId && s.BillDate >= todayStart && s.BillDate < todayEnd, cancellationToken);

        var yesterdayOrders = await _context.Sales
            .AsNoTracking()
            .CountAsync(s => s.LoginId == loginId && s.BillDate >= yesterdayStart && s.BillDate < yesterdayEnd, cancellationToken);

        var billAggregate = await _context.Sales
            .AsNoTracking()
            .Where(s => s.LoginId == loginId && s.BillDate >= rangeStart && s.BillDate < rangeEnd)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                BillCount = g.Count(),
                TotalGrand = g.Sum(x => x.GrandTotal)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totalProfit = await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale != null && i.Sale.LoginId == loginId && i.Sale.BillDate >= rangeStart && i.Sale.BillDate < rangeEnd)
            .GroupBy(_ => 1)
            .Select(g => g.Sum(x => (x.Price - x.PurchasePrice) * x.Quantity))
            .FirstOrDefaultAsync(cancellationToken);

        var repeatCustomers = await _context.Sales
            .AsNoTracking()
            .Where(s => s.LoginId == loginId && s.BillDate >= rangeStart && s.BillDate < rangeEnd)
            .GroupBy(s => s.CustomerFk)
            .CountAsync(g => g.Count() > 1, cancellationToken);

        var newCustomers = await _context.Sales
            .AsNoTracking()
            .Where(s => s.LoginId == loginId)
            .GroupBy(s => s.CustomerFk)
            .Select(g => g.Min(x => x.BillDate))
            .CountAsync(firstBillDate => firstBillDate >= rangeStart && firstBillDate < rangeEnd, cancellationToken);

        var billCount = billAggregate?.BillCount ?? 0;
        var totalGrand = billAggregate?.TotalGrand ?? 0m;

        return new DashboardStatisticsReadModel
        {
            TodayOrders = todayOrders,
            YesterdayOrders = yesterdayOrders,
            AverageBillValue = billCount == 0 ? 0 : decimal.Round(totalGrand / billCount, 2),
            AverageProfitPerBill = billCount == 0 ? 0 : decimal.Round(totalProfit / billCount, 2),
            RepeatCustomers = repeatCustomers,
            NewCustomers = newCustomers
        };
    }

    public async Task<DashboardStockValueSummaryReadModel> GetStockValueSummaryAsync(Guid loginId, CancellationToken cancellationToken)
    {
        var stockValues = await _context.Products
            .AsNoTracking()
            .Where(p => p.LoginId == loginId && !p.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                PurchaseStockValue = g.Sum(x => x.StockQuantity * x.PurchasePrice),
                SellingStockValue = g.Sum(x => x.StockQuantity * x.SellingPrice)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var purchaseValue = stockValues?.PurchaseStockValue ?? 0m;
        var sellingValue = stockValues?.SellingStockValue ?? 0m;

        return new DashboardStockValueSummaryReadModel
        {
            PurchaseStockValue = purchaseValue,
            SellingStockValue = sellingValue,
            ExpectedProfit = sellingValue - purchaseValue
        };
    }

    private static IReadOnlyCollection<DashboardGraphPointReadModel> BuildLast7DaysGraph(IEnumerable<DashboardGraphPointReadModel> points, DateTime endDate)
    {
        var source = points.ToDictionary(x => x.Date.Date, x => x.Amount);
        var startDate = endDate.AddDays(-6);

        var result = new List<DashboardGraphPointReadModel>(7);
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            result.Add(new DashboardGraphPointReadModel
            {
                Date = date,
                Amount = source.TryGetValue(date.Date, out var amount) ? amount : 0m
            });
        }

        return result;
    }
}
