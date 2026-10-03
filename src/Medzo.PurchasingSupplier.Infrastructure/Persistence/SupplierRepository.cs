using Medzo.PurchasingSupplier.Application.Suppliers;
using Medzo.PurchasingSupplier.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace Medzo.PurchasingSupplier.Infrastructure.Persistence;

public sealed class SupplierRepository(PurchasingSupplierDbContext database) : ISupplierRepository
{
    public Task<bool> ExistsWithNameOrEmailAsync(string normalizedName, string normalizedEmail, Guid? excludingId, CancellationToken cancellationToken) => database.Suppliers.AnyAsync(x => (!excludingId.HasValue || x.Id != excludingId) && (x.NormalizedName == normalizedName || x.NormalizedEmail == normalizedEmail), cancellationToken);
    public async Task<IReadOnlyList<Supplier>> ListAsync(bool activeOnly, CancellationToken cancellationToken) => await database.Suppliers.Where(x => !activeOnly || x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
    public Task<Supplier?> GetAsync(Guid id, CancellationToken cancellationToken) => database.Suppliers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task AddAsync(Supplier supplier, CancellationToken cancellationToken) => database.Suppliers.AddAsync(supplier, cancellationToken).AsTask();
    public Task SaveChangesAsync(CancellationToken cancellationToken) => database.SaveChangesAsync(cancellationToken);
}
