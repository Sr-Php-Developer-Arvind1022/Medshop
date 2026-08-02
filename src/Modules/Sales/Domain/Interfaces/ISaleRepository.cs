using Medshop.Modules.Sales.Domain.Entities;

namespace Medshop.Modules.Sales.Domain.Interfaces;

public interface ISaleRepository
{
    Task<string> GenerateNextBillNoAsync(Guid loginId, CancellationToken cancellationToken);
    Task AddAsync(Sale sale, CancellationToken cancellationToken);
    Task AddItemsAsync(IEnumerable<SaleItem> items, CancellationToken cancellationToken);
    Task<Sale?> GetByIdAndLoginIdAsync(long saleIdPk, Guid loginId, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<Sale> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? paymentMode,
        string? customerMobile,
        DateTime? fromDate,
        DateTime? toDate,
        Guid loginId,
        CancellationToken cancellationToken);
    Task DeleteAsync(Sale sale, CancellationToken cancellationToken);
    Task<decimal> SumGrandTotalAsync(DateTime fromDate, DateTime toDate, Guid loginId, CancellationToken cancellationToken);
    Task<decimal> SumSubtotalAsync(DateTime fromDate, DateTime toDate, Guid loginId, CancellationToken cancellationToken);
    Task<int> CountBillsAsync(DateTime fromDate, DateTime toDate, Guid loginId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Sale>> GetRecentAsync(Guid loginId, int take, CancellationToken cancellationToken);
}
