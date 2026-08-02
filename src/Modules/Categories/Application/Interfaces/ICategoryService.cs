using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Categories.Application.DTOs.Request;
using Medshop.Modules.Categories.Application.DTOs.Response;

namespace Medshop.Modules.Categories.Application.Interfaces;

public interface ICategoryService
{
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, Guid loginId, CancellationToken cancellationToken);
    Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, Guid loginId, CancellationToken cancellationToken);
    Task SoftDeleteAsync(Guid id, Guid loginId, CancellationToken cancellationToken);
    Task<CategoryResponse> GetByIdAsync(Guid id, Guid loginId, CancellationToken cancellationToken);
    Task<PagedResult<CategoryResponse>> GetPagedAsync(GetCategoriesRequest request, Guid loginId, CancellationToken cancellationToken);
}
