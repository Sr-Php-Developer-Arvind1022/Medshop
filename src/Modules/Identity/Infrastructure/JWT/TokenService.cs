using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Medshop.Modules.Identity.Domain.Entities;
using Medshop.Modules.Identity.Application.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Medshop.Modules.Identity.Infrastructure.JWT;

public class TokenService
{
    private const string AccessTokenType = "access";
    private const string RefreshTokenType = "refresh";
    private const string ResetTokenType = "password_reset";

    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettingsOptions)
    {
        _jwtSettings = jwtSettingsOptions.Value;
    }

    public AuthTokenResult GenerateAccessToken(User user)
        => GenerateToken(user, AccessTokenType, DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes));

    public AuthTokenResult GenerateRefreshToken(User user)
        => GenerateToken(user, RefreshTokenType, DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays));

    public AuthTokenResult GeneratePasswordResetToken(User user)
        => GenerateToken(user, ResetTokenType, DateTime.UtcNow.AddMinutes(_jwtSettings.PasswordResetTokenExpiryMinutes));

    public ClaimsPrincipal ValidateRefreshToken(string refreshToken)
        => ValidateToken(refreshToken, RefreshTokenType);

    public ClaimsPrincipal ValidatePasswordResetToken(string resetToken)
        => ValidateToken(resetToken, ResetTokenType);

    private AuthTokenResult GenerateToken(User user, string tokenType, DateTime expiresAtUtc)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtClaimTypes.LoginId, user.Id.ToString()),
            new Claim(JwtClaimTypes.TokenType, tokenType)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AuthTokenResult
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAtUtc
        };
    }

    private ClaimsPrincipal ValidateToken(string token, string expectedTokenType)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
            var tokenType = principal.FindFirstValue(JwtClaimTypes.TokenType);

            if (!string.Equals(tokenType, expectedTokenType, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Invalid token type.");
            }

            return principal;
        }
        catch (SecurityTokenException)
        {
            throw new UnauthorizedAccessException("Invalid or expired token.");
        }
        catch (ArgumentException)
        {
            throw new UnauthorizedAccessException("Invalid or expired token.");
        }
    }
}

public class AuthTokenResult
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
