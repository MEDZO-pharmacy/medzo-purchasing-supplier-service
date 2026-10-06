namespace Medzo.PurchasingSupplier.Domain.PurchaseOrders;

using Medzo.PurchasingSupplier.Domain.Suppliers;

public sealed class PurchaseOrder
{
    private PurchaseOrder() { }

    public PurchaseOrder(Guid supplierId, string orderNumber, IReadOnlyCollection<PurchaseOrderItem> items)
    {
        if (supplierId == Guid.Empty) throw new ArgumentException("Supplier is required.");
        if (string.IsNullOrWhiteSpace(orderNumber)) throw new ArgumentException("Order number is required.");
        if (items.Count == 0) throw new ArgumentException("At least one medicine item is required.");

        Id = Guid.NewGuid();
        SupplierId = supplierId;
        OrderNumber = orderNumber;
        Status = PurchaseOrderStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        Items = items.ToList();
    }

    public Guid Id { get; private set; }
    public Guid SupplierId { get; private set; }
    public Supplier Supplier { get; private set; } = null!;
    public string OrderNumber { get; private set; } = null!;
    public PurchaseOrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public ICollection<PurchaseOrderItem> Items { get; private set; } = new List<PurchaseOrderItem>();
}

public sealed class PurchaseOrderItem
{
    private PurchaseOrderItem() { }

    public PurchaseOrderItem(Guid medicineId, string medicineName, int quantity)
    {
        if (medicineId == Guid.Empty) throw new ArgumentException("Medicine is required.");
        if (string.IsNullOrWhiteSpace(medicineName)) throw new ArgumentException("Medicine name is required.");
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");

        Id = Guid.NewGuid();
        MedicineId = medicineId;
        MedicineName = medicineName.Trim();
        Quantity = quantity;
    }

    public Guid Id { get; private set; }
    public Guid PurchaseOrderId { get; private set; }
    public Guid MedicineId { get; private set; }
    public string MedicineName { get; private set; } = null!;
    public int Quantity { get; private set; }
}

public enum PurchaseOrderStatus { Pending }
