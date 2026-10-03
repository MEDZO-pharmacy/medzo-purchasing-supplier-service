namespace Medzo.PurchasingSupplier.Application.Suppliers;

public sealed record CreateSupplierRequest(string Name, string ContactName, string Email, string Phone, string? Address, bool AllowDuplicate = false);
public sealed record UpdateSupplierRequest(string Name, string ContactName, string Email, string Phone, string? Address);
public sealed record SupplierResponse(Guid Id, string Name, string ContactName, string Email, string Phone, string? Address, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? DeactivatedAtUtc);

public interface ISupplierService
{
    Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<SupplierResponse>> ListAsync(bool activeOnly, CancellationToken cancellationToken);
    Task<SupplierResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<SupplierResponse> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteInactiveAsync(Guid id, CancellationToken cancellationToken);
}
public interface ISupplierRepository
{
    Task<bool> ExistsWithNameOrEmailAsync(string normalizedName, string normalizedEmail, Guid? excludingId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Domain.Suppliers.Supplier>> ListAsync(bool activeOnly, CancellationToken cancellationToken);
    Task<Domain.Suppliers.Supplier?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Domain.Suppliers.Supplier supplier, CancellationToken cancellationToken);
    void Remove(Domain.Suppliers.Supplier supplier);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
