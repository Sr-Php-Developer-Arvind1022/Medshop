using Medshop.Modules.Identity.Persistence;
using Medshop.Modules.Sales.Domain.Entities;
using Medshop.Modules.Sales.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Medshop.Modules.Sales.Infrastructure.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly MedshopDbContext _context;

    public SaleRepository(MedshopDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateNextBillNoAsync(Guid loginId, CancellationToken cancellationToken)
    {
        var nextNumber = (await _context.Sales
            .Where(s => s.LoginId == loginId)
            .Select(s => (long?)s.SaleIdPk)
            .MaxAsync(cancellationToken) ?? 0) + 1;

        return $"INV{nextNumber:000000}";
    }

    public async Task AddAsync(Sale sale, CancellationToken cancellationToken)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddItemsAsync(IEnumerable<SaleItem> items, CancellationToken cancellationToken)
    {
        await _context.SaleItems.AddRangeAsync(items, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Sale?> GetByIdAndLoginIdAsync(long saleIdPk, Guid loginId, CancellationToken cancellationToken)
        => await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.SaleIdPk == saleIdPk && s.LoginId == loginId, cancellationToken);

    public async Task<(IReadOnlyCollection<Sale> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? paymentMode,
        string? customerMobile,
        DateTime? fromDate,
        DateTime? toDate,
        Guid loginId,
        CancellationToken cancellationToken)
    {
        var query = _context.Sales
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .Where(s => s.LoginId == loginId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLower();
            query = query.Where(s => s.BillNo.ToLower().Contains(normalized)
                || s.Customer!.Name.ToLower().Contains(normalized)
                || s.Customer.Mobile.ToLower().Contains(normalized));
        }

        if (!string.IsNullOrWhiteSpace(paymentMode))
        {
            var normalizedPayment = paymentMode.Trim().ToLower();
            query = query.Where(s => s.PaymentMode.ToLower() == normalizedPayment);
        }

        if (!string.IsNullOrWhiteSpace(customerMobile))
        {
            var normalizedMobile = customerMobile.Trim().ToLower();
            query = query.Where(s => s.Customer!.Mobile.ToLower().Contains(normalizedMobile));
        }

        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;
            query = query.Where(s => s.BillDate >= from);
        }

        if (toDate.HasValue)
        {
            var to = toDate.Value.Date.AddDays(1);
            query = query.Where(s => s.BillDate < to);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.SaleIdPk)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task DeleteAsync(Sale sale, CancellationToken cancellationToken)
    {
        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> SumGrandTotalAsync(DateTime fromDate, DateTime toDate, Guid loginId, CancellationToken cancellationToken)
        => await _context.Sales
            .Where(s => s.LoginId == loginId && s.BillDate >= fromDate && s.BillDate < toDate)
            .SumAsync(s => (decimal?)s.GrandTotal, cancellationToken) ?? 0;

    public async Task<decimal> SumSubtotalAsync(DateTime fromDate, DateTime toDate, Guid loginId, CancellationToken cancellationToken)
        => await _context.Sales
            .Where(s => s.LoginId == loginId && s.BillDate >= fromDate && s.BillDate < toDate)
            .SumAsync(s => (decimal?)s.Subtotal, cancellationToken) ?? 0;

    public async Task<int> CountBillsAsync(DateTime fromDate, DateTime toDate, Guid loginId, CancellationToken cancellationToken)
        => await _context.Sales
            .CountAsync(s => s.LoginId == loginId && s.BillDate >= fromDate && s.BillDate < toDate, cancellationToken);

    public async Task<IReadOnlyCollection<Sale>> GetRecentAsync(Guid loginId, int take, CancellationToken cancellationToken)
        => await _context.Sales
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .Where(s => s.LoginId == loginId)
            .OrderByDescending(s => s.SaleIdPk)
            .Take(take)
            .ToListAsync(cancellationToken);
}
