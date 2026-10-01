using Medzo.PurchasingSupplier.Infrastructure;
using Medzo.PurchasingSupplier.Api.Configuration;
using Medzo.PurchasingSupplier.Api.ExceptionHandling;
using Medzo.PurchasingSupplier.Application.Suppliers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Medzo.PurchasingSupplier.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

LocalEnvironmentFile.LoadFromCurrentDirectory();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSecret = builder.Configuration["Jwt:Secret"];
if (!string.IsNullOrWhiteSpace(jwtSecret))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new()
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), RoleClaimType = ClaimTypes.Role
    });
}
builder.Services.AddAuthorization(options => options.AddPolicy("SupplierManage", policy => policy.RequireRole("InventoryManager", "Admin")));

var app = builder.Build();

if (string.Equals(builder.Configuration["Database:Provider"], "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<PurchasingSupplierDbContext>().Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
