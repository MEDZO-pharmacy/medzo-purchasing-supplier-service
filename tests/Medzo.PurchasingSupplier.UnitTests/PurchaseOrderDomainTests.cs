using Medzo.PurchasingSupplier.Domain.PurchaseOrders;
using Xunit;

namespace Medzo.PurchasingSupplier.UnitTests;

public sealed class PurchaseOrderDomainTests
{
    [Fact]
    public void Constructor_creates_a_pending_order_with_trimmed_item_details()
    {
        var supplierId = Guid.NewGuid();
        var medicineId = Guid.NewGuid();
        var item = new PurchaseOrderItem(medicineId, " Aspirin 100mg ", 12);

        var order = new PurchaseOrder(supplierId, "PO-001", [item]);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(supplierId, order.SupplierId);
        Assert.Equal("PO-001", order.OrderNumber);
        Assert.Equal(PurchaseOrderStatus.Pending, order.Status);
        Assert.Null(order.ReceivedAtUtc);
        Assert.Equal("Aspirin 100mg", item.MedicineName);
        Assert.Equal(12, item.Quantity);
    }

    [Fact]
    public void Rejects_invalid_order_and_item_details()
    {
        var validItem = new PurchaseOrderItem(Guid.NewGuid(), "Aspirin", 1);
        var invalidActions = new Action[]
        {
            () => new PurchaseOrder(Guid.Empty, "PO-001", [validItem]),
            () => new PurchaseOrder(Guid.NewGuid(), " ", [validItem]),
            () => new PurchaseOrder(Guid.NewGuid(), "PO-001", []),
            () => new PurchaseOrderItem(Guid.Empty, "Aspirin", 1),
            () => new PurchaseOrderItem(Guid.NewGuid(), " ", 1),
            () => new PurchaseOrderItem(Guid.NewGuid(), "Aspirin", 0)
        };

        foreach (var action in invalidActions)
            Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void MarkReceived_transitions_once_and_rejects_a_repeat()
    {
        var item = new PurchaseOrderItem(Guid.NewGuid(), "Aspirin", 2);
        var order = new PurchaseOrder(Guid.NewGuid(), "PO-001", [item]);

        order.MarkReceived();
        var receivedAt = order.ReceivedAtUtc;

        Assert.Equal(PurchaseOrderStatus.Received, order.Status);
        Assert.NotNull(receivedAt);
        Assert.Throws<InvalidOperationException>(order.MarkReceived);
        Assert.Equal(receivedAt, order.ReceivedAtUtc);
    }
}
