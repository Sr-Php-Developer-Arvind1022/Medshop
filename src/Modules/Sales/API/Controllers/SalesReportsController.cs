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
[Route("api/reports/sales")]
public class SalesReportsController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SalesReportsController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    [HttpGet("today")]
    public async Task<ActionResult<ApiResponse<SalesReportResponse>>> Today(CancellationToken cancellationToken)
    {
        var response = await _saleService.GetTodayReportAsync(GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<SalesReportResponse>.SuccessResult(response, "Today report fetched successfully"));
    }

    [HttpGet("yesterday")]
    public async Task<ActionResult<ApiResponse<SalesReportResponse>>> Yesterday(CancellationToken cancellationToken)
    {
        var response = await _saleService.GetYesterdayReportAsync(GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<SalesReportResponse>.SuccessResult(response, "Yesterday report fetched successfully"));
    }

    [HttpGet("last-7-days")]
    public async Task<ActionResult<ApiResponse<SalesReportResponse>>> Last7Days(CancellationToken cancellationToken)
    {
        var response = await _saleService.GetLast7DaysReportAsync(GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<SalesReportResponse>.SuccessResult(response, "Last 7 days report fetched successfully"));
    }

    [HttpGet("last-30-days")]
    public async Task<ActionResult<ApiResponse<SalesReportResponse>>> Last30Days(CancellationToken cancellationToken)
    {
        var response = await _saleService.GetLast30DaysReportAsync(GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<SalesReportResponse>.SuccessResult(response, "Last 30 days report fetched successfully"));
    }

    [HttpGet("this-year")]
    public async Task<ActionResult<ApiResponse<SalesReportResponse>>> ThisYear(CancellationToken cancellationToken)
    {
        var response = await _saleService.GetThisYearReportAsync(GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<SalesReportResponse>.SuccessResult(response, "This year report fetched successfully"));
    }

    [HttpGet("custom")]
    public async Task<ActionResult<ApiResponse<SalesReportResponse>>> Custom([FromQuery] CustomSalesReportRequest request, CancellationToken cancellationToken)
    {
        var response = await _saleService.GetCustomReportAsync(request, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<SalesReportResponse>.SuccessResult(response, "Custom report fetched successfully"));
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
