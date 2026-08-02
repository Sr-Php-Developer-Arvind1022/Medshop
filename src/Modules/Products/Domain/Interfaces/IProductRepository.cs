using Medshop.Modules.Products.Domain.Entities;

namespace Medshop.Modules.Products.Domain.Interfaces;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken);
    Task<Product?> GetByPrimaryKeyAsync(long productIdPk, CancellationToken cancellationToken);
    Task<Product?> GetByPrimaryKeyAndLoginIdAsync(long productIdPk, Guid loginId, CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Product?> GetByIdAndLoginIdAsync(Guid id, Guid loginId, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? category,
        decimal? minPurchasePrice,
        decimal? maxPurchasePrice,
        decimal? minSellingPrice,
        decimal? maxSellingPrice,
        Guid? loginId,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Product>> SearchByLoginIdAsync(Guid loginId, string? keyword, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Product>> SearchByNameAsync(Guid loginId, string? name, CancellationToken cancellationToken);
    Task UpdateAsync(Product product, CancellationToken cancellationToken);
}