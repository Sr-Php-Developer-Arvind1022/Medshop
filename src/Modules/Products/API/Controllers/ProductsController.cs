using System.Security.Claims;
using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Identity.Infrastructure.JWT;
using Medshop.Modules.Products.Application.DTOs.Request;
using Medshop.Modules.Products.Application.DTOs.Response;
using Medshop.Modules.Products.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medshop.Modules.Products.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Create([FromForm] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var response = await _productService.CreateAsync(request, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<ProductResponse>.SuccessResult(response, "Product created successfully"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Update(Guid id, [FromForm] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var response = await _productService.UpdateAsync(id, request, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<ProductResponse>.SuccessResult(response, "Product updated successfully"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        await _productService.SoftDeleteAsync(id, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResult(null, "Product deleted successfully"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductResponse>>>> GetPaged([FromQuery] GetProductsRequest request, CancellationToken cancellationToken)
    {
        request.LoginId = GetCurrentLoginId();
        var response = await _productService.GetPagedAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProductResponse>>.SuccessResult(response, "Products fetched successfully"));
    }

    [HttpGet("search-medicine-by-login-id")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProductResponse>>>> SearchMedicineByLoginId([FromQuery] string? keyword, CancellationToken cancellationToken)
    {
        var response = await _productService.SearchMedicinesByLoginIdAsync(keyword, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<ProductResponse>>.SuccessResult(response, "Products fetched successfully"));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProductResponse>>>> SearchByName([FromQuery] string? name, CancellationToken cancellationToken)
    {
        var response = await _productService.SearchByNameAsync(name, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<ProductResponse>>.SuccessResult(response, "Products fetched successfully"));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _productService.GetByIdAndLoginIdAsync(id, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<ProductResponse>.SuccessResult(response, "Product fetched successfully"));
    }

    [HttpGet("{id:guid}/login/{loginId:guid}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> GetByIdAndLoginId(Guid id, Guid loginId, CancellationToken cancellationToken)
    {
        _ = loginId;
        var response = await _productService.GetByIdAndLoginIdAsync(id, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<ProductResponse>.SuccessResult(response, "Product fetched successfully"));
    }

    private Guid GetCurrentLoginId()
    {
        var loginIdValue = User.FindFirstValue(JwtClaimTypes.LoginId);
        if (!Guid.TryParse(loginIdValue, out var loginId))
        {
            throw new UnauthorizedAccessException("Login id claim is missing or invalid.");
        }

        return loginId;
    }
}