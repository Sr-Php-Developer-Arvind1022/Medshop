using System.Security.Claims;
using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Identity.Infrastructure.JWT;
using Medshop.Modules.Sales.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Medshop.Modules.Sales.Application.DTOs.Response;

namespace Medshop.Modules.Sales.API.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Returns complete medical store dashboard data in a single response.
    /// </summary>
    /// <param name="filter">today, yesterday, week, month, year, custom</param>
    /// <param name="startDate">Required when filter=custom (yyyy-MM-dd)</param>
    /// <param name="endDate">Required when filter=custom (yyyy-MM-dd)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete dashboard payload</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> Get(
        [FromQuery] string filter = "today",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _dashboardService.GetDashboardAsync(GetCurrentLoginId(), filter, startDate, endDate, cancellationToken);
        return Ok(ApiResponse<DashboardDto>.SuccessResult(response, "Dashboard Loaded Successfully"));
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
