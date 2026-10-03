using Medzo.PurchasingSupplier.Domain.Suppliers;

namespace Medzo.PurchasingSupplier.Application.Suppliers;

public sealed class SupplierService(ISupplierRepository repository) : ISupplierService
{
    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        Validate(request.Name, request.ContactName, request.Email, request.Phone);
        var normalizedName = request.Name.Trim().ToUpperInvariant();
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        if (!request.AllowDuplicate && await repository.ExistsWithNameOrEmailAsync(normalizedName, normalizedEmail, null, cancellationToken))
            throw new SupplierDuplicateException();
        var supplier = new Supplier(request.Name, request.ContactName, request.Email, request.Phone, request.Address);
        await repository.AddAsync(supplier, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(supplier);
    }

    public async Task<IReadOnlyList<SupplierResponse>> ListAsync(bool activeOnly, CancellationToken cancellationToken) =>
        (await repository.ListAsync(activeOnly, cancellationToken)).Select(ToResponse).ToList();

    public async Task<SupplierResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await FindAsync(id, cancellationToken));

    public async Task<SupplierResponse> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        Validate(request.Name, request.ContactName, request.Email, request.Phone);
        var supplier = await FindAsync(id, cancellationToken);
        if (await repository.ExistsWithNameOrEmailAsync(request.Name.Trim().ToUpperInvariant(), request.Email.Trim().ToUpperInvariant(), id, cancellationToken))
            throw new SupplierDuplicateException();
        supplier.Update(request.Name, request.ContactName, request.Email, request.Phone, request.Address);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(supplier);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await FindAsync(id, cancellationToken);
        supplier.Deactivate();
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Supplier> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetAsync(id, cancellationToken) ?? throw new SupplierNotFoundException();

    private static SupplierResponse ToResponse(Supplier supplier) => new(supplier.Id, supplier.Name, supplier.ContactName, supplier.Email, supplier.Phone, supplier.Address, supplier.IsActive, supplier.CreatedAtUtc, supplier.DeactivatedAtUtc);

    private static void Validate(string name, string contactName, string email, string phone)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(name)) errors["name"] = ["Supplier name is required."];
        if (string.IsNullOrWhiteSpace(contactName)) errors["contactName"] = ["Contact name is required."];
        if (string.IsNullOrWhiteSpace(email) || !System.Net.Mail.MailAddress.TryCreate(email, out _)) errors["email"] = ["A valid email address is required."];
        if (string.IsNullOrWhiteSpace(phone)) errors["phone"] = ["Phone number is required."];
        if (errors.Count > 0) throw new SupplierValidationException(errors);
    }
}

public sealed class SupplierValidationException(IReadOnlyDictionary<string, string[]> errors) : Exception("Supplier details are invalid.") { public IReadOnlyDictionary<string, string[]> Errors { get; } = errors; }
public sealed class SupplierDuplicateException() : Exception("A supplier with the same name or email already exists. Review the record before saving a duplicate.");
public sealed class SupplierNotFoundException() : Exception("Supplier not found.");
