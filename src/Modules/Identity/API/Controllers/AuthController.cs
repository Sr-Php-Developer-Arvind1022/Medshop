using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Identity.Application.DTOs.Request;
using Medshop.Modules.Identity.Application.DTOs.Response;
using Medshop.Modules.Identity.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Medshop.Modules.Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register([FromForm] RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(ApiResponse<RegisterResponse>.SuccessResult(response, "User registered successfully"));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "User logged in successfully"));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponse>>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<ForgotPasswordResponse>.SuccessResult(response, "Password reset token generated successfully"));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResult(null, "Password reset successfully"));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<RefreshTokenResponse>>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request, cancellationToken);
        return Ok(ApiResponse<RefreshTokenResponse>.SuccessResult(response, "Token refreshed successfully"));
    }
}
