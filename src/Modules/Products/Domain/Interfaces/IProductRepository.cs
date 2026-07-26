using Medshop.Modules.Products.Domain.Entities;

namespace Medshop.Modules.Products.Domain.Interfaces;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Product?> GetByIdAndLoginIdAsync(Guid id, Guid loginId, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        Guid? loginId,
        CancellationToken cancellationToken);
    Task UpdateAsync(Product product, CancellationToken cancellationToken);
}