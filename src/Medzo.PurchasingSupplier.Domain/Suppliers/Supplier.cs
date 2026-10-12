namespace Medzo.PurchasingSupplier.Domain.Suppliers;

public sealed class Supplier
{
    public const int MaxNameLength = 200;
    public const int MaxContactNameLength = 150;
    public const int MaxEmailLength = 254;
    public const int MaxPhoneLength = 30;
    public const int MaxAddressLength = 500;

    private Supplier() { }

    public Supplier(string name, string contactName, string email, string phone, string? address)
    {
        Id = Guid.NewGuid();
        Update(name, contactName, email, phone, address);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public string ContactName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public string? Address { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? DeactivatedAtUtc { get; private set; }

    public void Update(string name, string contactName, string email, string phone, string? address)
    {
        Name = Required(name, MaxNameLength, "Supplier name");
        ContactName = Required(contactName, MaxContactNameLength, "Contact name");
        Email = Required(email, MaxEmailLength, "Email");
        Phone = Required(phone, MaxPhoneLength, "Phone");
        Address = Optional(address, MaxAddressLength, "Address");
        NormalizedName = Name.ToUpperInvariant();
        NormalizedEmail = Email.ToUpperInvariant();
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        DeactivatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string Required(string value, int maximum, string label)
    {
        var cleaned = value?.Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) throw new ArgumentException($"{label} is required.");
        if (cleaned.Length > maximum) throw new ArgumentException($"{label} cannot exceed {maximum} characters.");
        return cleaned;
    }
    private static string? Optional(string? value, int maximum, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = value.Trim();
        if (cleaned.Length > maximum) throw new ArgumentException($"{label} cannot exceed {maximum} characters.");
        return cleaned;
    }
}
