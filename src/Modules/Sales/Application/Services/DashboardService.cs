using AutoMapper;
using Medshop.Modules.Sales.Application.DTOs.Response;
using Medshop.Modules.Sales.Application.Interfaces;
using Medshop.Modules.Sales.Domain.Interfaces;

namespace Medshop.Modules.Sales.Application.Services;

public class DashboardService : IDashboardService
{
    private const int LowStockThreshold = 10;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IMapper _mapper;

    public DashboardService(IDashboardRepository dashboardRepository, IMapper mapper)
    {
        _dashboardRepository = dashboardRepository;
        _mapper = mapper;
    }

    public async Task<DashboardDto> GetDashboardAsync(
        Guid loginId,
        string? filter,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var now = ToUtcDate(DateTime.UtcNow.Date);
        var (rangeStart, rangeEndExclusive) = ResolveRange(filter, startDate, endDate, now);
        var (graphStart, graphEndExclusive) = ResolveGraphRange(filter, startDate, endDate, now);

        var todayStart = now;
        var todayEnd = now.AddDays(1);
        var yesterdayStart = now.AddDays(-1);
        var yesterdayEnd = now;

        var cards = await _dashboardRepository.GetCardsAsync(loginId, todayStart, todayEnd, LowStockThreshold, cancellationToken);
        var stockSummary = await _dashboardRepository.GetStockSummaryAsync(loginId, rangeStart, rangeEndExclusive, cancellationToken);
        var lowStock = await _dashboardRepository.GetLowStockProductsAsync(loginId, LowStockThreshold, 20, cancellationToken);
        var outOfStock = await _dashboardRepository.GetOutOfStockProductsAsync(loginId, 20, cancellationToken);
        var expiry = await _dashboardRepository.GetExpiryProductsAsync(loginId, now, now.AddDays(30), 20, cancellationToken);
        var salesGraph = await _dashboardRepository.GetSalesGraphAsync(loginId, graphStart, graphEndExclusive, cancellationToken);
        var purchaseGraph = await _dashboardRepository.GetPurchaseGraphAsync(loginId, graphStart, graphEndExclusive, cancellationToken);
        var topSelling = await _dashboardRepository.GetTopSellingProductsAsync(loginId, rangeStart, rangeEndExclusive, 10, cancellationToken);
        var recentSales = await _dashboardRepository.GetRecentSalesAsync(loginId, 10, cancellationToken);
        var statistics = await _dashboardRepository.GetStatisticsAsync(loginId, todayStart, todayEnd, yesterdayStart, yesterdayEnd, rangeStart, rangeEndExclusive, cancellationToken);
        var stockValue = await _dashboardRepository.GetStockValueSummaryAsync(loginId, cancellationToken);

        return new DashboardDto
        {
            Cards = _mapper.Map<DashboardCardsDto>(cards),
            StockSummary = _mapper.Map<DashboardStockSummaryDto>(stockSummary),
            LowStockProducts = _mapper.Map<IReadOnlyCollection<DashboardLowStockProductDto>>(lowStock),
            OutOfStockProducts = _mapper.Map<IReadOnlyCollection<DashboardOutOfStockProductDto>>(outOfStock),
            ExpiryProducts = _mapper.Map<IReadOnlyCollection<DashboardExpiryProductDto>>(expiry),
            SalesGraph = salesGraph
                .Select(x => new DashboardSalesGraphPointDto
                {
                    Date = x.Date,
                    Sales = x.Amount
                })
                .ToList(),
            PurchaseGraph = purchaseGraph
                .Select(x => new DashboardPurchaseGraphPointDto
                {
                    Date = x.Date,
                    PurchaseAmount = x.Amount
                })
                .ToList(),
            TopSellingProducts = _mapper.Map<IReadOnlyCollection<DashboardTopSellingProductDto>>(topSelling),
            RecentSales = _mapper.Map<IReadOnlyCollection<DashboardRecentSaleDto>>(recentSales),
            Statistics = _mapper.Map<DashboardStatisticsDto>(statistics),
            StockValueSummary = _mapper.Map<DashboardStockValueSummaryDto>(stockValue)
        };
    }

    private static (DateTime Start, DateTime EndExclusive) ResolveRange(string? filter, DateTime? startDate, DateTime? endDate, DateTime today)
    {
        var normalized = string.IsNullOrWhiteSpace(filter) ? "today" : filter.Trim().ToLowerInvariant();

        return normalized switch
        {
            "today" => (today, today.AddDays(1)),
            "yesterday" => (today.AddDays(-1), today),
            "week" => (today.AddDays(-6), today.AddDays(1)),
            "month" => (ToUtcDate(new DateTime(today.Year, today.Month, 1)), ToUtcDate(new DateTime(today.Year, today.Month, 1).AddMonths(1))),
            "year" => (ToUtcDate(new DateTime(today.Year, 1, 1)), ToUtcDate(new DateTime(today.Year, 1, 1).AddYears(1))),
            "custom" => ResolveCustomRange(startDate, endDate),
            _ => throw new ArgumentException("Invalid filter. Allowed values: today, yesterday, week, month, year, custom.")
        };
    }

    private static (DateTime Start, DateTime EndExclusive) ResolveGraphRange(string? filter, DateTime? startDate, DateTime? endDate, DateTime today)
    {
        var normalized = string.IsNullOrWhiteSpace(filter) ? "today" : filter.Trim().ToLowerInvariant();

        return normalized switch
        {
            "today" or "yesterday" or "week" => (today.AddDays(-6), today.AddDays(1)),
            "month" => (ToUtcDate(new DateTime(today.Year, today.Month, 1)), ToUtcDate(new DateTime(today.Year, today.Month, 1).AddMonths(1))),
            "year" => (ToUtcDate(new DateTime(today.Year, 1, 1)), ToUtcDate(new DateTime(today.Year, 1, 1).AddYears(1))),
            "custom" => ResolveCustomRange(startDate, endDate),
            _ => throw new ArgumentException("Invalid filter. Allowed values: today, yesterday, week, month, year, custom.")
        };
    }

    private static (DateTime Start, DateTime EndExclusive) ResolveCustomRange(DateTime? startDate, DateTime? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
        {
            throw new ArgumentException("startDate and endDate are required when filter=custom.");
        }

        var start = ToUtcDate(startDate.Value.Date);
        var end = ToUtcDate(endDate.Value.Date);

        if (end < start)
        {
            throw new ArgumentException("endDate must be greater than or equal to startDate.");
        }

        return (start, end.AddDays(1));
    }

    private static DateTime ToUtcDate(DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        return value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
    }
}
