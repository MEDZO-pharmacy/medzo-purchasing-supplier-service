namespace Medzo.PurchasingSupplier.Application.PurchaseOrders;

public sealed record CreatePurchaseOrderRequest(Guid SupplierId, IReadOnlyList<CreatePurchaseOrderItemRequest> Items);
public sealed record CreatePurchaseOrderItemRequest(Guid MedicineId, string MedicineName, int Quantity);
public sealed record ReceivePurchaseOrderRequest(IReadOnlyList<ReceivePurchaseOrderItemRequest> Items);
public sealed record ReceivePurchaseOrderItemRequest(Guid PurchaseOrderItemId, string BatchNumber, DateOnly ExpiryDate);
public sealed record PurchaseOrderItemResponse(Guid Id, Guid MedicineId, string MedicineName, int Quantity);
public sealed record PurchaseOrderResponse(Guid Id, string OrderNumber, Guid SupplierId, string SupplierName, string Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? ReceivedAtUtc, IReadOnlyList<PurchaseOrderItemResponse> Items);
public sealed record PurchaseOrderStockReceivedEvent(Guid EventId, string PurchaseOrderNumber, DateTimeOffset ReceivedAtUtc, IReadOnlyList<PurchaseOrderStockReceivedLine> Lines);
public sealed record PurchaseOrderStockReceivedLine(Guid MedicineId, int Quantity, string BatchNumber, DateOnly ExpiryDate);

public interface IPurchaseOrderService
{
    Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PurchaseOrderResponse>> ListAsync(CancellationToken cancellationToken);
    Task<PurchaseOrderResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<PurchaseOrderResponse> ReceiveAsync(Guid id, ReceivePurchaseOrderRequest request, CancellationToken cancellationToken);
}

public interface IPurchaseOrderEventPublisher { Task PublishReceivedAsync(PurchaseOrderStockReceivedEvent message, CancellationToken cancellationToken); }

public interface IPurchaseOrderRepository
{
    Task<Domain.Suppliers.Supplier?> GetSupplierAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Domain.PurchaseOrders.PurchaseOrder>> ListAsync(CancellationToken cancellationToken);
    Task<Domain.PurchaseOrders.PurchaseOrder?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Domain.PurchaseOrders.PurchaseOrder order, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
