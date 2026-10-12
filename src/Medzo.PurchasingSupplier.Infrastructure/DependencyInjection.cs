using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Medzo.PurchasingSupplier.Application.Suppliers;
using Medzo.PurchasingSupplier.Application.PurchaseOrders;
using Medzo.PurchasingSupplier.Infrastructure.Persistence;
using Medzo.PurchasingSupplier.Infrastructure.Messaging;

namespace Medzo.PurchasingSupplier.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var connection = configuration.GetConnectionString("PurchasingSupplier") ?? "Data Source=medzo_purchasing.dev.db";
        services.AddDbContext<PurchasingSupplierDbContext>(options =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)) options.UseSqlServer(connection);
            else options.UseSqlite(connection);
        });
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddSingleton<IPurchaseOrderEventPublisher, KafkaPurchaseOrderEventPublisher>();
        return services;
    }
}
