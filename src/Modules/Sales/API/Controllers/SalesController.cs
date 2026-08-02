using System.Security.Claims;
using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Identity.Infrastructure.JWT;
using Medshop.Modules.Sales.Application.DTOs.Request;
using Medshop.Modules.Sales.Application.DTOs.Response;
using Medshop.Modules.Sales.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medshop.Modules.Sales.API.Controllers;

[ApiController]
[Authorize]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SaleResponse>>> Create([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var response = await _saleService.CreateAsync(request, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<SaleResponse>.SuccessResult(response, "Sale created successfully"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SaleResponse>>>> GetPaged([FromQuery] GetSalesRequest request, CancellationToken cancellationToken)
    {
        var response = await _saleService.GetPagedAsync(request, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<PagedResult<SaleResponse>>.SuccessResult(response, "Sales fetched successfully"));
    }

    [HttpGet("{salePk:long}")]
    public async Task<ActionResult<ApiResponse<SaleResponse>>> GetById(long salePk, CancellationToken cancellationToken)
    {
        var response = await _saleService.GetByIdAsync(salePk, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<SaleResponse>.SuccessResult(response, "Sale fetched successfully"));
    }

    [HttpDelete("{salePk:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long salePk, CancellationToken cancellationToken)
    {
        await _saleService.SoftDeleteAsync(salePk, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResult(null, "Sale deleted successfully"));
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
