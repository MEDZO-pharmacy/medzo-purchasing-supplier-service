namespace Medzo.PurchasingSupplier.Application.PurchaseOrders;

public sealed record CreatePurchaseOrderRequest(Guid SupplierId, IReadOnlyList<CreatePurchaseOrderItemRequest> Items);
public sealed record CreatePurchaseOrderItemRequest(Guid MedicineId, string MedicineName, int Quantity);
public sealed record PurchaseOrderItemResponse(Guid Id, Guid MedicineId, string MedicineName, int Quantity);
public sealed record PurchaseOrderResponse(Guid Id, string OrderNumber, Guid SupplierId, string Status, DateTimeOffset CreatedAtUtc, IReadOnlyList<PurchaseOrderItemResponse> Items);

public interface IPurchaseOrderService
{
    Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken);
}

public interface IPurchaseOrderRepository
{
    Task<Domain.Suppliers.Supplier?> GetSupplierAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Domain.PurchaseOrders.PurchaseOrder order, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
