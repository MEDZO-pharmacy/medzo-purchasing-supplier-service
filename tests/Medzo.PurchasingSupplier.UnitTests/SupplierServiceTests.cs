using Medzo.PurchasingSupplier.Application.Suppliers;
using Medzo.PurchasingSupplier.Domain.Suppliers;
using Moq;

namespace Medzo.PurchasingSupplier.UnitTests;

public sealed class SupplierServiceTests
{
    [Theory]
    [InlineData("", "Jane", "jane@example.com", "0771234567")]
    [InlineData("Medzo Supplies", "", "jane@example.com", "0771234567")]
    [InlineData("Medzo Supplies", "Jane", "invalid-email", "0771234567")]
    [InlineData("Medzo Supplies", "Jane", "jane@example.com", "")]
    public async Task CreateAsync_rejects_invalid_supplier_data_before_calling_the_repository(
        string name, string contactName, string email, string phone)
    {
        var repository = new Mock<ISupplierRepository>(MockBehavior.Strict);
        var service = new SupplierService(repository.Object);

        await Assert.ThrowsAsync<SupplierValidationException>(() =>
            service.CreateAsync(new CreateSupplierRequest(name, contactName, email, phone, null), CancellationToken.None));

        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_normalizes_duplicate_lookup_and_saves_a_valid_supplier()
    {
        var repository = new Mock<ISupplierRepository>(MockBehavior.Strict);
        Supplier? savedSupplier = null;
        repository.Setup(r => r.ExistsWithNameOrEmailAsync("MEDZO SUPPLIES", "JANE@EXAMPLE.COM", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(r => r.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .Callback<Supplier, CancellationToken>((supplier, _) => savedSupplier = supplier)
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = new SupplierService(repository.Object);

        var response = await service.CreateAsync(
            new CreateSupplierRequest(" Medzo Supplies ", " Jane Doe ", " JANE@example.com ", " 0771234567 ", " Colombo "),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Medzo Supplies", response.Name);
        Assert.Equal("JANE@example.com", response.Email);
        Assert.NotNull(savedSupplier);
        Assert.Equal("MEDZO SUPPLIES", savedSupplier!.NormalizedName);
        repository.Verify(r => r.AddAsync(savedSupplier, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_does_not_save_when_the_supplier_already_exists()
    {
        var repository = new Mock<ISupplierRepository>(MockBehavior.Strict);
        repository.Setup(r => r.ExistsWithNameOrEmailAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new SupplierService(repository.Object);

        await Assert.ThrowsAsync<SupplierDuplicateException>(() =>
            service.CreateAsync(new CreateSupplierRequest("Medzo Supplies", "Jane", "jane@example.com", "0771234567", null), CancellationToken.None));

        repository.Verify(r => r.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
