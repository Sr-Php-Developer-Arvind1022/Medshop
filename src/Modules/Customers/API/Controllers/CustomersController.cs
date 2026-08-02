using System.Security.Claims;
using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Customers.Application.DTOs.Response;
using Medshop.Modules.Customers.Application.Interfaces;
using Medshop.Modules.Identity.Infrastructure.JWT;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medshop.Modules.Customers.API.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<CustomerResponse?>>> SearchByMobile([FromQuery] string mobile, CancellationToken cancellationToken)
    {
        var response = await _customerService.SearchByMobileAsync(mobile, GetCurrentLoginId(), cancellationToken);
        return Ok(ApiResponse<CustomerResponse?>.SuccessResult(response, response is null ? "Customer not found" : "Customer fetched successfully"));
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
