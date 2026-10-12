using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Medzo.PurchasingSupplier.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PurchasingSupplierDbContext>
{
    public PurchasingSupplierDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__PurchasingSupplier")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=medzo-purchasing-db;Trusted_Connection=True";

        var options = new DbContextOptionsBuilder<PurchasingSupplierDbContext>()
            .UseSqlServer(connection)
            .Options;

        return new PurchasingSupplierDbContext(options);
    }
}
