using Medzo.PurchasingSupplier.Infrastructure;
using Medzo.PurchasingSupplier.Api.Configuration;
using Medzo.PurchasingSupplier.Api.ExceptionHandling;
using Medzo.PurchasingSupplier.Application.Suppliers;
using Medzo.PurchasingSupplier.Application.PurchaseOrders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Medzo.PurchasingSupplier.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

LocalEnvironmentFile.LoadFromCurrentDirectory();

var builder = WebApplication.CreateBuilder(args);

var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
if (builder.Environment.IsProduction() &&
    !databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "Production requires Database:Provider=SqlServer and the existing Azure SQL connection.");
}

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0) policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (!string.IsNullOrWhiteSpace(jwtSecret))
{
    if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
    {
        throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required when JWT authentication is enabled.");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new()
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer, ValidAudience = jwtAudience,
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
    await EnsureSqlitePurchaseOrderTablesAsync(database);
}
else if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<PurchasingSupplierDbContext>();
    await database.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseCors();
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

static async Task EnsureSqlitePurchaseOrderTablesAsync(PurchasingSupplierDbContext database)
{
    await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS purchase_orders (Id TEXT NOT NULL CONSTRAINT PK_purchase_orders PRIMARY KEY, SupplierId TEXT NOT NULL, OrderNumber TEXT NOT NULL, Status TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL, ReceivedAtUtc TEXT NULL, CONSTRAINT FK_purchase_orders_suppliers_SupplierId FOREIGN KEY (SupplierId) REFERENCES suppliers (Id) ON DELETE RESTRICT);");
    await database.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_purchase_orders_OrderNumber ON purchase_orders (OrderNumber);");
    await database.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_purchase_orders_SupplierId ON purchase_orders (SupplierId);");
    await database.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS purchase_order_items (Id TEXT NOT NULL CONSTRAINT PK_purchase_order_items PRIMARY KEY, PurchaseOrderId TEXT NOT NULL, MedicineId TEXT NOT NULL, MedicineName TEXT NOT NULL, Quantity INTEGER NOT NULL, CONSTRAINT FK_purchase_order_items_purchase_orders_PurchaseOrderId FOREIGN KEY (PurchaseOrderId) REFERENCES purchase_orders (Id) ON DELETE CASCADE);");
    await database.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_purchase_order_items_PurchaseOrderId ON purchase_order_items (PurchaseOrderId);");
    var connection = database.Database.GetDbConnection();
    await connection.OpenAsync();
    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(purchase_orders);";
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync()) columns.Add(reader.GetString(1));
        if (!columns.Contains("ReceivedAtUtc"))
        {
            await using var addColumn = connection.CreateCommand();
            addColumn.CommandText = "ALTER TABLE purchase_orders ADD COLUMN ReceivedAtUtc TEXT NULL;";
            await addColumn.ExecuteNonQueryAsync();
        }
    }
    finally { await connection.CloseAsync(); }
}

public partial class Program;
