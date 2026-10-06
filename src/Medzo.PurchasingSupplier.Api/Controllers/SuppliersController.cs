using Medzo.PurchasingSupplier.Application.Suppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medzo.PurchasingSupplier.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize(Policy = "SupplierManage")]
public sealed class SuppliersController(ISupplierService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<SupplierResponse>> List([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default) => service.ListAsync(activeOnly, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<SupplierResponse> Get(Guid id, CancellationToken cancellationToken) => service.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<SupplierResponse>> Create(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await service.CreateAsync(request, cancellationToken);
        return Created($"/api/suppliers/{supplier.Id}", supplier);
    }

    [HttpPut("{id:guid}")]
    public Task<SupplierResponse> Update(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken) => service.UpdateAsync(id, request, cancellationToken);

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteInactive(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteInactiveAsync(id, cancellationToken);
        return NoContent();
    }
}
