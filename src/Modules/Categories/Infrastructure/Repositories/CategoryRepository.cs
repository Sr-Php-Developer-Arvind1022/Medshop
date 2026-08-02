using Medshop.Modules.Categories.Domain.Entities;
using Medshop.Modules.Categories.Domain.Interfaces;
using Medshop.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Medshop.Modules.Categories.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly MedshopDbContext _context;

    public CategoryRepository(MedshopDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        await _context.Categories.AddAsync(category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Category?> GetByIdAndLoginIdAsync(Guid id, Guid loginId, CancellationToken cancellationToken)
        => await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.LoginId == loginId && !c.IsDeleted, cancellationToken);

    public async Task<(IReadOnlyCollection<Category> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        bool? isActive,
        Guid loginId,
        CancellationToken cancellationToken)
    {
        var query = _context.Categories
            .AsNoTracking()
            .Where(c => c.LoginId == loginId && !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(normalizedSearch)
                || (c.Description != null && c.Description.ToLower().Contains(normalizedSearch)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
