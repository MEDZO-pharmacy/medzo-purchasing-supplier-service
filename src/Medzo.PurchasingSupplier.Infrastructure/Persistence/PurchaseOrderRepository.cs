using Medzo.PurchasingSupplier.Application.PurchaseOrders;
using Medzo.PurchasingSupplier.Domain.PurchaseOrders;
using Microsoft.EntityFrameworkCore;

namespace Medzo.PurchasingSupplier.Infrastructure.Persistence;

public sealed class PurchaseOrderRepository(PurchasingSupplierDbContext database) : IPurchaseOrderRepository
{
    public Task<Domain.Suppliers.Supplier?> GetSupplierAsync(Guid id, CancellationToken cancellationToken) => database.Suppliers.SingleOrDefaultAsync(supplier => supplier.Id == id, cancellationToken);
    public async Task<IReadOnlyList<PurchaseOrder>> ListAsync(CancellationToken cancellationToken) =>
        (await database.PurchaseOrders.AsNoTracking().Include(order => order.Supplier).Include(order => order.Items).ToListAsync(cancellationToken))
        .OrderByDescending(order => order.CreatedAtUtc)
        .ToList();
    public Task<PurchaseOrder?> GetAsync(Guid id, CancellationToken cancellationToken) => database.PurchaseOrders.Include(order => order.Supplier).Include(order => order.Items).SingleOrDefaultAsync(order => order.Id == id, cancellationToken);
    public Task AddAsync(PurchaseOrder order, CancellationToken cancellationToken) => database.PurchaseOrders.AddAsync(order, cancellationToken).AsTask();
    public Task SaveChangesAsync(CancellationToken cancellationToken) => database.SaveChangesAsync(cancellationToken);
}
