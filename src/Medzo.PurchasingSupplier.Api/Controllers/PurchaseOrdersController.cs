using Medzo.PurchasingSupplier.Application.PurchaseOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medzo.PurchasingSupplier.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize(Policy = "SupplierManage")]
public sealed class PurchaseOrdersController(IPurchaseOrderService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PurchaseOrderResponse>> List(CancellationToken cancellationToken) => service.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<PurchaseOrderResponse> Get(Guid id, CancellationToken cancellationToken) => service.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderResponse>> Create(CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await service.CreateAsync(request, cancellationToken);
        return Created($"/api/purchase-orders/{order.Id}", order);
    }
}
