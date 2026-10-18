using Medzo.PurchasingSupplier.Application.PurchaseOrders;
using Medzo.PurchasingSupplier.Domain.PurchaseOrders;
using Medzo.PurchasingSupplier.Domain.Suppliers;
using Moq;
using Xunit;

namespace Medzo.PurchasingSupplier.UnitTests;

public sealed class PurchaseOrderServiceMockTests
{
    private readonly Mock<IPurchaseOrderRepository> repository = new(MockBehavior.Strict);
    private readonly Mock<IPurchaseOrderEventPublisher> publisher = new(MockBehavior.Strict);
    private PurchaseOrderService Service() => new(repository.Object, publisher.Object);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Nonpositive_quantity_is_rejected_without_repository_or_event_calls(int quantity)
    {
        var request = new CreatePurchaseOrderRequest(Guid.NewGuid(), [new(Guid.NewGuid(), "Paracetamol", quantity)]);
        var exception = await Assert.ThrowsAsync<PurchaseOrderValidationException>(() => Service().CreateAsync(request, CancellationToken.None));
        Assert.Contains("items[0].quantity", exception.Errors.Keys);
        repository.VerifyNoOtherCalls();
        publisher.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_or_inactive_supplier_prevents_order_creation(bool inactive)
    {
        var supplier = new Supplier("Supplier", "Jane", "jane@example.com", "0771234567", null);
        supplier.Deactivate();
        repository.Setup(r => r.GetSupplierAsync(supplier.Id, It.IsAny<CancellationToken>())).ReturnsAsync(inactive ? supplier : null);
        var request = new CreatePurchaseOrderRequest(supplier.Id, [new(Guid.NewGuid(), "Medicine", 1)]);

        if (inactive) await Assert.ThrowsAsync<PurchaseOrderSupplierInactiveException>(() => Service().CreateAsync(request, CancellationToken.None));
        else await Assert.ThrowsAsync<PurchaseOrderSupplierNotFoundException>(() => Service().CreateAsync(request, CancellationToken.None));

        repository.Verify(r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        publisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Valid_order_is_saved_pending_and_does_not_publish_stock_received()
    {
        var supplier = new Supplier("Supplier", "Jane", "jane@example.com", "0771234567", null);
        var medicineId = Guid.NewGuid();
        repository.Setup(r => r.GetSupplierAsync(supplier.Id, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);
        repository.Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await Service().CreateAsync(new(supplier.Id, [new(medicineId, " Paracetamol ", 12)]), CancellationToken.None);

        Assert.Equal("PENDING", result.Status);
        Assert.Null(result.ReceivedAtUtc);
        Assert.Equal(supplier.Id, result.SupplierId);
        var line = Assert.Single(result.Items);
        Assert.Equal(medicineId, line.MedicineId);
        Assert.Equal("Paracetamol", line.MedicineName);
        Assert.Equal(12, line.Quantity);
        repository.Verify(r => r.AddAsync(It.Is<PurchaseOrder>(o => o.Items.Single().Quantity == 12), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        publisher.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Expired_receipt_is_rejected_without_saving_or_publishing(int days)
    {
        var order = new PurchaseOrder(Guid.NewGuid(), "PO-1", [new(Guid.NewGuid(), "Medicine", 10)]);
        repository.Setup(r => r.GetAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var request = new ReceivePurchaseOrderRequest([new(order.Items.Single().Id, "LOT-1", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(days))]);

        var exception = await Assert.ThrowsAsync<PurchaseOrderValidationException>(() => Service().ReceiveAsync(order.Id, request, CancellationToken.None));

        Assert.Contains("items[0].expiryDate", exception.Errors.Keys);
        Assert.Equal(PurchaseOrderStatus.Pending, order.Status);
        repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        publisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Received_order_cannot_be_received_or_published_again()
    {
        var order = new PurchaseOrder(Guid.NewGuid(), "PO-1", [new(Guid.NewGuid(), "Medicine", 10)]);
        order.MarkReceived();
        repository.Setup(r => r.GetAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        await Assert.ThrowsAsync<PurchaseOrderAlreadyReceivedException>(() => Service().ReceiveAsync(order.Id, new([]), CancellationToken.None));

        repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        publisher.VerifyNoOtherCalls();
    }
}
