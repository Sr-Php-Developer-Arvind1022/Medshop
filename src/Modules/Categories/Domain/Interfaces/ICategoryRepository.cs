using Medshop.Modules.Categories.Domain.Entities;

namespace Medshop.Modules.Categories.Domain.Interfaces;

public interface ICategoryRepository
{
    Task AddAsync(Category category, CancellationToken cancellationToken);
    Task<Category?> GetByIdAndLoginIdAsync(Guid id, Guid loginId, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<Category> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        bool? isActive,
        Guid loginId,
        CancellationToken cancellationToken);
    Task UpdateAsync(Category category, CancellationToken cancellationToken);
}
