using Medshop.Modules.Identity.Persistence;
using Medshop.Modules.Products.Domain.Entities;
using Medshop.Modules.Products.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Medshop.Modules.Products.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly MedshopDbContext _context;

    public ProductRepository(MedshopDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Product?> GetByPrimaryKeyAsync(long productIdPk, CancellationToken cancellationToken)
        => await _context.Products.FirstOrDefaultAsync(p => p.ProductIdPk == productIdPk && !p.IsDeleted, cancellationToken);

    public async Task<Product?> GetByPrimaryKeyAndLoginIdAsync(long productIdPk, Guid loginId, CancellationToken cancellationToken)
        => await _context.Products.FirstOrDefaultAsync(p => p.ProductIdPk == productIdPk && p.LoginId == loginId && !p.IsDeleted, cancellationToken);

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

    public async Task<Product?> GetByIdAndLoginIdAsync(Guid id, Guid loginId, CancellationToken cancellationToken)
        => await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.LoginId == loginId && !p.IsDeleted, cancellationToken);

    public async Task<(IReadOnlyCollection<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? category,
        decimal? minPurchasePrice,
        decimal? maxPurchasePrice,
        decimal? minSellingPrice,
        decimal? maxSellingPrice,
        Guid? loginId,
        CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(normalizedSearch)
                || (p.Description != null && p.Description.ToLower().Contains(normalizedSearch))
                || p.Category.ToLower().Contains(normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToLower();
            query = query.Where(p => p.Category.ToLower() == normalizedCategory);
        }

        if (minPurchasePrice.HasValue)
        {
            query = query.Where(p => p.PurchasePrice >= minPurchasePrice.Value);
        }

        if (maxPurchasePrice.HasValue)
        {
            query = query.Where(p => p.PurchasePrice <= maxPurchasePrice.Value);
        }

        if (minSellingPrice.HasValue)
        {
            query = query.Where(p => p.SellingPrice >= minSellingPrice.Value);
        }

        if (maxSellingPrice.HasValue)
        {
            query = query.Where(p => p.SellingPrice <= maxSellingPrice.Value);
        }

        if (loginId.HasValue)
        {
            query = query.Where(p => p.LoginId == loginId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyCollection<Product>> SearchByLoginIdAsync(Guid loginId, string? keyword, CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.LoginId == loginId && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(normalizedKeyword)
                || (p.Description != null && p.Description.ToLower().Contains(normalizedKeyword))
                || p.Category.ToLower().Contains(normalizedKeyword));
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Product>> SearchByNameAsync(Guid loginId, string? name, CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.LoginId == loginId && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(name))
        {
            var normalizedName = name.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(normalizedName));
        }

        return await query
            .OrderBy(p => p.Name)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }
}