using Medzo.PurchasingSupplier.Domain.PurchaseOrders;

namespace Medzo.PurchasingSupplier.Application.PurchaseOrders;

public sealed class PurchaseOrderService(IPurchaseOrderRepository repository) : IPurchaseOrderService
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
        return Map(order);
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

    private static PurchaseOrderResponse Map(PurchaseOrder order) => new(order.Id, order.OrderNumber, order.SupplierId, order.Status.ToString().ToUpperInvariant(), order.CreatedAtUtc, order.Items.Select(item => new PurchaseOrderItemResponse(item.Id, item.MedicineId, item.MedicineName, item.Quantity)).ToList());
}

public sealed class PurchaseOrderValidationException(IReadOnlyDictionary<string, string[]> errors) : Exception("Purchase order details are invalid.") { public IReadOnlyDictionary<string, string[]> Errors { get; } = errors; }
public sealed class PurchaseOrderSupplierNotFoundException() : Exception("The selected supplier is unavailable.");
public sealed class PurchaseOrderSupplierInactiveException() : Exception("The selected supplier is inactive and cannot receive new purchase orders.");
