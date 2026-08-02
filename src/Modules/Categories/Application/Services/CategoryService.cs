using AutoMapper;
using FluentValidation;
using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Categories.Application.DTOs.Request;
using Medshop.Modules.Categories.Application.DTOs.Response;
using Medshop.Modules.Categories.Application.Interfaces;
using Medshop.Modules.Categories.Domain.Entities;
using Medshop.Modules.Categories.Domain.Interfaces;

namespace Medshop.Modules.Categories.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateCategoryRequest> _createValidator;
    private readonly IValidator<UpdateCategoryRequest> _updateValidator;
    private readonly IWebHostEnvironment _environment;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IMapper mapper,
        IValidator<CreateCategoryRequest> createValidator,
        IValidator<UpdateCategoryRequest> updateValidator,
        IWebHostEnvironment environment)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _environment = environment;
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, Guid loginId, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var category = _mapper.Map<Category>(request);
        category.LoginId = loginId;
        category.CategoryImage = await SaveCategoryImageAsync(request.CategoryImage, cancellationToken);

        await _categoryRepository.AddAsync(category, cancellationToken);

        return _mapper.Map<CategoryResponse>(category);
    }

    public async Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, Guid loginId, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var category = await _categoryRepository.GetByIdAndLoginIdAsync(id, loginId, cancellationToken);
        if (category is null)
        {
            throw new KeyNotFoundException("Category not found for this login id.");
        }

        category.Name = request.Name;
        category.Description = request.Description;
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        if (request.CategoryImage is not null)
        {
            DeleteCategoryImage(category.CategoryImage);
            category.CategoryImage = await SaveCategoryImageAsync(request.CategoryImage, cancellationToken);
        }

        await _categoryRepository.UpdateAsync(category, cancellationToken);

        return _mapper.Map<CategoryResponse>(category);
    }

    public async Task SoftDeleteAsync(Guid id, Guid loginId, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAndLoginIdAsync(id, loginId, cancellationToken);
        if (category is null)
        {
            throw new KeyNotFoundException("Category not found for this login id.");
        }

        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepository.UpdateAsync(category, cancellationToken);
    }

    public async Task<CategoryResponse> GetByIdAsync(Guid id, Guid loginId, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAndLoginIdAsync(id, loginId, cancellationToken);
        if (category is null)
        {
            throw new KeyNotFoundException("Category not found for this login id.");
        }

        return _mapper.Map<CategoryResponse>(category);
    }

    public async Task<PagedResult<CategoryResponse>> GetPagedAsync(GetCategoriesRequest request, Guid loginId, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);

        var (items, totalCount) = await _categoryRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            request.Search,
            request.IsActive,
            loginId,
            cancellationToken);

        return new PagedResult<CategoryResponse>
        {
            Items = _mapper.Map<IReadOnlyCollection<CategoryResponse>>(items),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private async Task<string?> SaveCategoryImageAsync(IFormFile? categoryImage, CancellationToken cancellationToken)
    {
        if (categoryImage is null || categoryImage.Length == 0)
        {
            return null;
        }

        var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "Uploads", "Categories");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(categoryImage.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await categoryImage.CopyToAsync(stream, cancellationToken);

        return fileName;
    }

    private void DeleteCategoryImage(string? existingImage)
    {
        if (string.IsNullOrWhiteSpace(existingImage))
        {
            return;
        }

        var filePath = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "Uploads", "Categories", existingImage);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
