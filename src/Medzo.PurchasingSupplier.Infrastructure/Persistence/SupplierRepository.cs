using Medzo.PurchasingSupplier.Application.Suppliers;
using Medzo.PurchasingSupplier.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace Medzo.PurchasingSupplier.Infrastructure.Persistence;

public sealed class SupplierRepository(PurchasingSupplierDbContext database) : ISupplierRepository
{
    public Task<bool> ExistsWithNameOrEmailAsync(string normalizedName, string normalizedEmail, CancellationToken cancellationToken) => database.Suppliers.AnyAsync(x => x.NormalizedName == normalizedName || x.NormalizedEmail == normalizedEmail, cancellationToken);
    public Task AddAsync(Supplier supplier, CancellationToken cancellationToken) => database.Suppliers.AddAsync(supplier, cancellationToken).AsTask();
    public Task SaveChangesAsync(CancellationToken cancellationToken) => database.SaveChangesAsync(cancellationToken);
}
