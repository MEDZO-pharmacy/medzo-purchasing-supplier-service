using Medzo.PurchasingSupplier.Domain.Suppliers;
using Xunit;

namespace Medzo.PurchasingSupplier.UnitTests;

public sealed class SupplierDomainTests
{
    [Fact]
    public void Constructor_trims_and_normalizes_supplier_details()
    {
        var supplier = new Supplier(" Medzo Labs ", " Nimal Perera ", " Sales@Medzo.lk ", " 0771234567 ", " 12 Main Street ");

        Assert.NotEqual(Guid.Empty, supplier.Id);
        Assert.Equal("Medzo Labs", supplier.Name);
        Assert.Equal("MEDZO LABS", supplier.NormalizedName);
        Assert.Equal("Nimal Perera", supplier.ContactName);
        Assert.Equal("Sales@Medzo.lk", supplier.Email);
        Assert.Equal("SALES@MEDZO.LK", supplier.NormalizedEmail);
        Assert.Equal("0771234567", supplier.Phone);
        Assert.Equal("12 Main Street", supplier.Address);
        Assert.True(supplier.IsActive);
    }

    [Fact]
    public void Update_replaces_normalized_details_and_clears_blank_address()
    {
        var supplier = ValidSupplier();

        supplier.Update(" New Name ", " New Contact ", " NEW@example.com ", " 0112345678 ", " ");

        Assert.Equal("New Name", supplier.Name);
        Assert.Equal("NEW NAME", supplier.NormalizedName);
        Assert.Equal("NEW@example.com", supplier.Email);
        Assert.Equal("NEW@EXAMPLE.COM", supplier.NormalizedEmail);
        Assert.Null(supplier.Address);
    }

    [Fact]
    public void Rejects_missing_or_overlong_supplier_details()
    {
        var invalidActions = new Action[]
        {
            () => new Supplier(" ", "Contact", "a@example.com", "071", null),
            () => new Supplier("Name", " ", "a@example.com", "071", null),
            () => new Supplier("Name", "Contact", " ", "071", null),
            () => new Supplier("Name", "Contact", "a@example.com", " ", null),
            () => new Supplier(new string('n', Supplier.MaxNameLength + 1), "Contact", "a@example.com", "071", null),
            () => new Supplier("Name", "Contact", "a@example.com", "071", new string('a', Supplier.MaxAddressLength + 1))
        };

        foreach (var action in invalidActions)
            Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Deactivate_is_idempotent_and_preserves_the_first_timestamp()
    {
        var supplier = ValidSupplier();

        supplier.Deactivate();
        var firstTimestamp = supplier.DeactivatedAtUtc;
        supplier.Deactivate();

        Assert.False(supplier.IsActive);
        Assert.NotNull(firstTimestamp);
        Assert.Equal(firstTimestamp, supplier.DeactivatedAtUtc);
    }

    private static Supplier ValidSupplier() => new("Supplier", "Contact", "supplier@example.com", "0712345678", null);
}
