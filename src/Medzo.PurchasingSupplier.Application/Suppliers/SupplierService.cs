using Medzo.PurchasingSupplier.Domain.Suppliers;

namespace Medzo.PurchasingSupplier.Application.Suppliers;

public sealed class SupplierService(ISupplierRepository repository) : ISupplierService
{
    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var normalizedName = request.Name.Trim().ToUpperInvariant();
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        if (!request.AllowDuplicate && await repository.ExistsWithNameOrEmailAsync(normalizedName, normalizedEmail, cancellationToken))
            throw new SupplierDuplicateException();
        var supplier = new Supplier(request.Name, request.ContactName, request.Email, request.Phone, request.Address);
        await repository.AddAsync(supplier, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return new(supplier.Id, supplier.Name, supplier.ContactName, supplier.Email, supplier.Phone, supplier.Address, supplier.CreatedAtUtc);
    }

    private static void Validate(CreateSupplierRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name)) errors["name"] = ["Supplier name is required."];
        if (string.IsNullOrWhiteSpace(request.ContactName)) errors["contactName"] = ["Contact name is required."];
        if (string.IsNullOrWhiteSpace(request.Email) || !System.Net.Mail.MailAddress.TryCreate(request.Email, out _)) errors["email"] = ["A valid email address is required."];
        if (string.IsNullOrWhiteSpace(request.Phone)) errors["phone"] = ["Phone number is required."];
        if (errors.Count > 0) throw new SupplierValidationException(errors);
    }
}

public sealed class SupplierValidationException(IReadOnlyDictionary<string, string[]> errors) : Exception("Supplier details are invalid.") { public IReadOnlyDictionary<string, string[]> Errors { get; } = errors; }
public sealed class SupplierDuplicateException() : Exception("A supplier with the same name or email already exists. Review the record before saving a duplicate.");
