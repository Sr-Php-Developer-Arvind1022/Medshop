using AutoMapper;
using BCrypt.Net;
using FluentValidation;
using Medshop.Modules.Identity.Application.DTOs.Request;
using Medshop.Modules.Identity.Application.DTOs.Response;
using Medshop.Modules.Identity.Application.Interfaces;
using Medshop.Modules.Identity.Domain.Entities;
using Medshop.Modules.Identity.Domain.Interfaces;
using Medshop.Modules.Identity.Infrastructure.JWT;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Medshop.Modules.Identity.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly JwtSettings _jwtSettings;
    private readonly IWebHostEnvironment _environment;
    private readonly TokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IMapper mapper,
        IValidator<RegisterRequest> registerValidator,
        IOptions<JwtSettings> jwtSettingsOptions,
        IWebHostEnvironment environment,
        TokenService tokenService)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _registerValidator = registerValidator;
        _jwtSettings = jwtSettingsOptions.Value;
        _environment = environment;
        _tokenService = tokenService;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Email", "Email already exists.") });
        }

        if (await _userRepository.ExistsByMobileAsync(request.Mobile, cancellationToken))
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Mobile", "Mobile already exists.") });
        }

        var fileName = await SaveProfileImageAsync(request.ProfileImage, cancellationToken);

        var user = _mapper.Map<User>(request);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.ProfileImage = fileName;

        await _userRepository.AddAsync(user, cancellationToken);

        return _mapper.Map<RegisterResponse>(user);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Email) || user is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException("Password is required.");
        }
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user);

        return new LoginResponse
        {
            LoginID = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Token = accessToken.Token,
            RefreshToken = refreshToken.Token,
            TokenExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc
        };
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Email", "Email is required.") });
        }

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Email", "No user found with this email.") });
        }

        var resetToken = _tokenService.GeneratePasswordResetToken(user);

        return new ForgotPasswordResponse
        {
            Email = user.Email,
            ResetToken = resetToken.Token,
            ResetTokenExpiresAtUtc = resetToken.ExpiresAtUtc
        };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ResetToken))
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("ResetToken", "Reset token is required.") });
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("NewPassword", "New password is required.") });
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("ConfirmPassword", "Passwords do not match.") });
        }

        var principal = _tokenService.ValidatePasswordResetToken(request.ResetToken);
        var email = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException("Invalid reset token.");
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Email", "No user found with this email.") });
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdatePasswordAsync(email, passwordHash, cancellationToken);
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("RefreshToken", "Refresh token is required.") });
        }

        var principal = _tokenService.ValidateRefreshToken(request.RefreshToken);
        var email = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user);

        return new RefreshTokenResponse
        {
            Token = accessToken.Token,
            RefreshToken = refreshToken.Token,
            TokenExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc
        };
    }

    private async Task<string?> SaveProfileImageAsync(IFormFile? profileImage, CancellationToken cancellationToken)
    {
        if (profileImage is null || profileImage.Length == 0)
        {
            return null;
        }

        var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "Uploads", "Profile");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(profileImage.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await profileImage.CopyToAsync(stream, cancellationToken);
        return fileName;
    }
}

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; }
    public int RefreshTokenExpiryDays { get; set; } = 7;
    public int PasswordResetTokenExpiryMinutes { get; set; } = 15;
}
