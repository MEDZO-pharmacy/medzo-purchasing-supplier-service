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
        ValidateRequiredText(request.Name, "name", "Supplier name", Supplier.MaxNameLength, errors);
        ValidateRequiredText(request.ContactName, "contactName", "Contact person", Supplier.MaxContactNameLength, errors);

        var trimmedEmail = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedEmail))
            errors["email"] = ["Email address is required."];
        else if (trimmedEmail.Length > Supplier.MaxEmailLength)
            errors["email"] = [$"Email address cannot exceed {Supplier.MaxEmailLength} characters."];
        else if (!System.Net.Mail.MailAddress.TryCreate(trimmedEmail, out var parsedEmail) || !string.Equals(parsedEmail.Address, trimmedEmail, StringComparison.OrdinalIgnoreCase))
            errors["email"] = ["Enter a valid email address."];

        var trimmedPhone = request.Phone?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedPhone))
            errors["phone"] = ["Phone number is required."];
        else if (trimmedPhone.Length > Supplier.MaxPhoneLength)
            errors["phone"] = [$"Phone number cannot exceed {Supplier.MaxPhoneLength} characters."];
        else
        {
            var phoneDigits = trimmedPhone.Count(char.IsDigit);
            if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedPhone, "^[0-9+()\\-\\s]+$") || phoneDigits != 10)
                errors["phone"] = ["Enter a valid 10-digit phone number."];
        }

        if (!string.IsNullOrWhiteSpace(request.Address) && request.Address.Trim().Length > Supplier.MaxAddressLength)
            errors["address"] = [$"Business address cannot exceed {Supplier.MaxAddressLength} characters."];

        if (errors.Count > 0) throw new SupplierValidationException(errors);
    }

    private static void ValidateRequiredText(string? value, string field, string label, int maximumLength, IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors[field] = [$"{label} is required."];
        else if (value.Trim().Length > maximumLength)
            errors[field] = [$"{label} cannot exceed {maximumLength} characters."];
    }
}

public sealed class SupplierValidationException(IReadOnlyDictionary<string, string[]> errors) : Exception("Supplier details are invalid.") { public IReadOnlyDictionary<string, string[]> Errors { get; } = errors; }
public sealed class SupplierDuplicateException() : Exception("A supplier with the same name or email already exists. Review the record before saving a duplicate.");
