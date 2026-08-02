using System.Security.Claims;
using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Categories.Application.DTOs.Request;
using Medshop.Modules.Categories.Application.DTOs.Response;
using Medshop.Modules.Categories.Application.Interfaces;
using Medshop.Modules.Identity.Infrastructure.JWT;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medshop.Modules.Categories.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Create([FromForm] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await _categoryService.CreateAsync(request, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<CategoryResponse>.SuccessResult(response, "Category created successfully"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Update(Guid id, [FromForm] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await _categoryService.UpdateAsync(id, request, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<CategoryResponse>.SuccessResult(response, "Category updated successfully"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        await _categoryService.SoftDeleteAsync(id, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResult(null, "Category deleted successfully"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CategoryResponse>>>> GetPaged([FromQuery] GetCategoriesRequest request, CancellationToken cancellationToken)
    {
        var response = await _categoryService.GetPagedAsync(request, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<PagedResult<CategoryResponse>>.SuccessResult(response, "Categories fetched successfully"));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _categoryService.GetByIdAsync(id, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<CategoryResponse>.SuccessResult(response, "Category fetched successfully"));
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
