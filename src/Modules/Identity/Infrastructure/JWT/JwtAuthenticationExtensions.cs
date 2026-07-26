using System.Text;
using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Identity.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Medshop.Modules.Identity.Infrastructure.JWT;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        var message = ResolveUnauthorizedMessage(context);
                        var response = ApiResponse<object>.FailureResult(message);

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(response);
                    },
                    OnForbidden = async context =>
                    {
                        var response = ApiResponse<object>.FailureResult("You are not authorized to access this resource.");

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(response);
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static string ResolveUnauthorizedMessage(JwtBearerChallengeContext context)
    {
        if (context.AuthenticateFailure is SecurityTokenExpiredException)
        {
            return "Token has expired.";
        }

        var authorizationHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return "Token is required.";
        }

        return "Invalid token.";
    }
}
