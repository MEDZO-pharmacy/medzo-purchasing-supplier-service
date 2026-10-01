namespace Medzo.PurchasingSupplier.Application.Suppliers;

public sealed record CreateSupplierRequest(string Name, string ContactName, string Email, string Phone, string? Address, bool AllowDuplicate = false);
public sealed record SupplierResponse(Guid Id, string Name, string ContactName, string Email, string Phone, string? Address, DateTimeOffset CreatedAtUtc);

public interface ISupplierService { Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken); }
public interface ISupplierRepository
{
    Task<bool> ExistsWithNameOrEmailAsync(string normalizedName, string normalizedEmail, CancellationToken cancellationToken);
    Task AddAsync(Domain.Suppliers.Supplier supplier, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
