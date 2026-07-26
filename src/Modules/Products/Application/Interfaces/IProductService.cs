using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Products.Application.DTOs.Request;
using Medshop.Modules.Products.Application.DTOs.Response;

namespace Medshop.Modules.Products.Application.Interfaces;

public interface IProductService
{
    Task<ProductResponse> CreateAsync(CreateProductRequest request, Guid loginId, CancellationToken cancellationToken);
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, Guid loginId, CancellationToken cancellationToken);
    Task SoftDeleteAsync(Guid id, Guid loginId, CancellationToken cancellationToken);
    Task<PagedResult<ProductResponse>> GetPagedAsync(GetProductsRequest request, CancellationToken cancellationToken);
    Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductResponse> GetByIdAndLoginIdAsync(Guid id, Guid loginId, CancellationToken cancellationToken);
}