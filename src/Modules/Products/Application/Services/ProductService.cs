using AutoMapper;
using FluentValidation;
using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Products.Application.DTOs.Request;
using Medshop.Modules.Products.Application.DTOs.Response;
using Medshop.Modules.Products.Application.Interfaces;
using Medshop.Modules.Products.Domain.Entities;
using Medshop.Modules.Products.Domain.Interfaces;

namespace Medshop.Modules.Products.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;
    private readonly IWebHostEnvironment _environment;

    public ProductService(
        IProductRepository productRepository,
        IMapper mapper,
        IValidator<CreateProductRequest> createValidator,
        IValidator<UpdateProductRequest> updateValidator,
        IWebHostEnvironment environment)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _environment = environment;
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, Guid loginId, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var product = _mapper.Map<Product>(request);
        product.LoginId = loginId;
        product.ProductImage = await SaveProductImageAsync(request.ProductImage, cancellationToken);

        await _productRepository.AddAsync(product, cancellationToken);

        return _mapper.Map<ProductResponse>(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, Guid loginId, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var product = await _productRepository.GetByIdAndLoginIdAsync(id, loginId, cancellationToken);
        if (product is null)
        {
            throw new KeyNotFoundException("Product not found for this login id.");
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Category = request.Category;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.UpdatedAt = DateTime.UtcNow;

        if (request.ProductImage is not null)
        {
            DeleteProductImage(product.ProductImage);
            product.ProductImage = await SaveProductImageAsync(request.ProductImage, cancellationToken);
        }

        await _productRepository.UpdateAsync(product, cancellationToken);

        return _mapper.Map<ProductResponse>(product);
    }

    public async Task SoftDeleteAsync(Guid id, Guid loginId, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAndLoginIdAsync(id, loginId, cancellationToken);
        if (product is null)
        {
            throw new KeyNotFoundException("Product not found for this login id.");
        }

        product.IsDeleted = true;
        product.DeletedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product, cancellationToken);
    }

    public async Task<PagedResult<ProductResponse>> GetPagedAsync(GetProductsRequest request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);

        var (items, totalCount) = await _productRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            request.Search,
            request.Category,
            request.MinPrice,
            request.MaxPrice,
            request.LoginId,
            cancellationToken);

        return new PagedResult<ProductResponse>
        {
            Items = _mapper.Map<IReadOnlyCollection<ProductResponse>>(items),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            throw new KeyNotFoundException("Product not found.");
        }

        return _mapper.Map<ProductResponse>(product);
    }

    public async Task<ProductResponse> GetByIdAndLoginIdAsync(Guid id, Guid loginId, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAndLoginIdAsync(id, loginId, cancellationToken);
        if (product is null)
        {
            throw new KeyNotFoundException("Product not found for this login id.");
        }

        return _mapper.Map<ProductResponse>(product);
    }

    private async Task<string?> SaveProductImageAsync(IFormFile? productImage, CancellationToken cancellationToken)
    {
        if (productImage is null || productImage.Length == 0)
        {
            return null;
        }

        var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "Uploads", "Products");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(productImage.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await productImage.CopyToAsync(stream, cancellationToken);

        return fileName;
    }

    private void DeleteProductImage(string? existingImage)
    {
        if (string.IsNullOrWhiteSpace(existingImage))
        {
            return;
        }

        var filePath = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "Uploads", "Products", existingImage);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}