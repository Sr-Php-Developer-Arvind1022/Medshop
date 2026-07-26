using FluentValidation;
using Medshop.BuildingBlocks.Infrastructure.Middleware;
using Medshop.Modules.Identity.API.Controllers;
using Medshop.Modules.Identity.Application.DTOs.Request;
using Medshop.Modules.Identity.Application.Interfaces;
using Medshop.Modules.Identity.Application.Mapping;
using Medshop.Modules.Identity.Application.Services;
using Medshop.Modules.Identity.Application.Validators;
using Medshop.Modules.Identity.Domain.Interfaces;
using Medshop.Modules.Identity.Infrastructure.JWT;
using Medshop.Modules.Identity.Infrastructure.Repositories;
using Medshop.Modules.Identity.Persistence;
using Medshop.Modules.Products.Application.DTOs.Request;
using Medshop.Modules.Products.Application.Interfaces;
using Medshop.Modules.Products.Application.Services;
using Medshop.Modules.Products.Application.Validators;
using Medshop.Modules.Products.Domain.Interfaces;
using Medshop.Modules.Products.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;

try
{
    Console.WriteLine("===== APPLICATION STARTING =====");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console());

    // Register Controllers
    builder.Services
        .AddControllers()
        .AddApplicationPart(typeof(AuthController).Assembly);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Medshop API",
            Version = "v1"
        });

        var jwtSecurityScheme = new OpenApiSecurityScheme
        {
            Scheme = "bearer",
            BearerFormat = "JWT",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Description = "Paste only the JWT token here. Swagger will send it as Bearer {token}.",
            Reference = new OpenApiReference
            {
                Id = JwtBearerDefaults.AuthenticationScheme,
                Type = ReferenceType.SecurityScheme
            }
        };

        options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [jwtSecurityScheme] = Array.Empty<string>()
        });
    });

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

    builder.Services.AddDbContext<MedshopDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddAutoMapper(typeof(IdentityProfile).Assembly);

    builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
    builder.Services.AddScoped<IValidator<CreateProductRequest>, CreateProductRequestValidator>();
    builder.Services.AddScoped<IValidator<UpdateProductRequest>, UpdateProductRequestValidator>();

    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<TokenService>();

    builder.Services.AddJwtAuthentication(builder.Configuration);

    Console.WriteLine("Services Registered");

    var app = builder.Build();

    Console.WriteLine("App Build Completed");

    await EnsureDatabaseExistsAsync(connectionString);

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<MedshopDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Medshop API v1");
        options.RoutePrefix = "swagger";
    });

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Test endpoint
    app.MapGet("/", () => "Medshop API Running");

    Console.WriteLine("===== REGISTERED ENDPOINTS =====");

    var provider = app.Services.GetRequiredService<IActionDescriptorCollectionProvider>();

    foreach (var action in provider.ActionDescriptors.Items)
    {
        Console.WriteLine(action.DisplayName);
    }

    Console.WriteLine("Before app.Run()");

    app.Run();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("========== STARTUP ERROR ==========");
    Console.WriteLine(ex);
    Console.ResetColor();
    throw;
}

static async Task EnsureDatabaseExistsAsync(string connectionString)
{
    var builder = new NpgsqlConnectionStringBuilder(connectionString);
    var databaseName = builder.Database;

    if (string.IsNullOrWhiteSpace(databaseName))
    {
        return;
    }

    var adminConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Database = "postgres"
    }.ConnectionString;

    await using var connection = new NpgsqlConnection(adminConnectionString);
    await connection.OpenAsync();

    await using var command = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'", connection);
    var exists = await command.ExecuteScalarAsync();

    if (exists is null)
    {
        await using var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
        await createCommand.ExecuteNonQueryAsync();
    }
}