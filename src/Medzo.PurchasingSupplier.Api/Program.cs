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
    var database = scope.ServiceProvider.GetRequiredService<PurchasingSupplierDbContext>();
    await database.Database.EnsureCreatedAsync();
    await EnsureSqliteSupplierColumnsAsync(database);
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static async Task EnsureSqliteSupplierColumnsAsync(PurchasingSupplierDbContext database)
{
    var connection = database.Database.GetDbConnection();
    await connection.OpenAsync();
    try
    {
        await using var columnsCommand = connection.CreateCommand();
        columnsCommand.CommandText = "PRAGMA table_info(suppliers);";
        await using var reader = await columnsCommand.ExecuteReaderAsync();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync()) columns.Add(reader.GetString(1));
        if (!columns.Contains("IsActive"))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "ALTER TABLE suppliers ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 1;";
            await command.ExecuteNonQueryAsync();
        }
        if (!columns.Contains("DeactivatedAtUtc"))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "ALTER TABLE suppliers ADD COLUMN DeactivatedAtUtc TEXT NULL;";
            await command.ExecuteNonQueryAsync();
        }
    }
    finally { await connection.CloseAsync(); }
}

public partial class Program;
