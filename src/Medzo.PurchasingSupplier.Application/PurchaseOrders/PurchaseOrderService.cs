using Medzo.PurchasingSupplier.Domain.PurchaseOrders;

namespace Medzo.PurchasingSupplier.Application.PurchaseOrders;

public sealed class PurchaseOrderService(IPurchaseOrderRepository repository, IPurchaseOrderEventPublisher eventPublisher) : IPurchaseOrderService
{
    public async Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0) throw new PurchaseOrderValidationException(errors);

        var supplier = await repository.GetSupplierAsync(request.SupplierId, cancellationToken) ?? throw new PurchaseOrderSupplierNotFoundException();
        if (!supplier.IsActive) throw new PurchaseOrderSupplierInactiveException();

        var number = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var items = request.Items.Select(item => new PurchaseOrderItem(item.MedicineId, item.MedicineName, item.Quantity)).ToList();
        var order = new PurchaseOrder(supplier.Id, number, items);
        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(order, supplier.Name);
    }

    public async Task<IReadOnlyList<PurchaseOrderResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken)).Select(order => Map(order, order.Supplier.Name)).ToList();

    public async Task<PurchaseOrderResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await repository.GetAsync(id, cancellationToken) ?? throw new PurchaseOrderNotFoundException();
        return Map(order, order.Supplier.Name);
    }

    public async Task<PurchaseOrderResponse> ReceiveAsync(Guid id, ReceivePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await repository.GetAsync(id, cancellationToken) ?? throw new PurchaseOrderNotFoundException();
        if (order.Status == PurchaseOrderStatus.Received) throw new PurchaseOrderAlreadyReceivedException();

        var errors = ValidateReceipt(order, request);
        if (errors.Count > 0) throw new PurchaseOrderValidationException(errors);

        order.MarkReceived();
        await repository.SaveChangesAsync(cancellationToken);

        var receiptItems = request.Items.ToDictionary(item => item.PurchaseOrderItemId);
        var message = new PurchaseOrderStockReceivedEvent(Guid.NewGuid(), order.OrderNumber, order.ReceivedAtUtc!.Value,
            order.Items.Select(item => new PurchaseOrderStockReceivedLine(item.MedicineId, item.Quantity, receiptItems[item.Id].BatchNumber.Trim(), receiptItems[item.Id].ExpiryDate)).ToList());
        await eventPublisher.PublishReceivedAsync(message, cancellationToken);
        return Map(order, order.Supplier.Name);
    }

    private static Dictionary<string, string[]> Validate(CreatePurchaseOrderRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.SupplierId == Guid.Empty) errors["supplierId"] = ["Select an active supplier."];
        if (request.Items is null || request.Items.Count == 0) errors["items"] = ["Add at least one medicine item."];
        else
        {
            for (var index = 0; index < request.Items.Count; index++)
            {
                var item = request.Items[index];
                if (item.MedicineId == Guid.Empty) errors[$"items[{index}].medicineId"] = ["Select a medicine."];
                if (string.IsNullOrWhiteSpace(item.MedicineName)) errors[$"items[{index}].medicineName"] = ["Medicine name is required."];
                if (item.Quantity <= 0) errors[$"items[{index}].quantity"] = ["Quantity must be greater than zero."];
            }
        }
        return errors;
    }

    private static Dictionary<string, string[]> ValidateReceipt(PurchaseOrder order, ReceivePurchaseOrderRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.Items is null || request.Items.Count != order.Items.Count) errors["items"] = ["Provide receipt details for every purchase order item."];
        else
        {
            var expected = order.Items.Select(item => item.Id).ToHashSet();
            var received = request.Items.Select(item => item.PurchaseOrderItemId).ToList();
            if (received.Distinct().Count() != received.Count || received.Any(itemId => !expected.Contains(itemId))) errors["items"] = ["Receipt items must match this purchase order."];
            for (var index = 0; index < request.Items.Count; index++)
            {
                var item = request.Items[index];
                if (string.IsNullOrWhiteSpace(item.BatchNumber)) errors[$"items[{index}].batchNumber"] = ["Batch number is required."];
                else if (item.BatchNumber.Trim().Length > 100) errors[$"items[{index}].batchNumber"] = ["Batch number cannot exceed 100 characters."];
                if (item.ExpiryDate <= DateOnly.FromDateTime(DateTime.UtcNow)) errors[$"items[{index}].expiryDate"] = ["Expiry date must be in the future."];
            }
        }
        return errors;
    }

    private static PurchaseOrderResponse Map(PurchaseOrder order, string supplierName) => new(order.Id, order.OrderNumber, order.SupplierId, supplierName, order.Status.ToString().ToUpperInvariant(), order.CreatedAtUtc, order.ReceivedAtUtc, order.Items.Select(item => new PurchaseOrderItemResponse(item.Id, item.MedicineId, item.MedicineName, item.Quantity)).ToList());
}

public sealed class PurchaseOrderValidationException(IReadOnlyDictionary<string, string[]> errors) : Exception("Purchase order details are invalid.") { public IReadOnlyDictionary<string, string[]> Errors { get; } = errors; }
public sealed class PurchaseOrderSupplierNotFoundException() : Exception("The selected supplier is unavailable.");
public sealed class PurchaseOrderSupplierInactiveException() : Exception("The selected supplier is inactive and cannot receive new purchase orders.");
public sealed class PurchaseOrderNotFoundException() : Exception("Purchase order not found.");
public sealed class PurchaseOrderAlreadyReceivedException() : Exception("This purchase order has already been received.");
