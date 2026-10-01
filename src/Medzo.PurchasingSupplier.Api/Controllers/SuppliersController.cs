using Medzo.PurchasingSupplier.Application.Suppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medzo.PurchasingSupplier.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize(Policy = "SupplierManage")]
public sealed class SuppliersController(ISupplierService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SupplierResponse>> Create(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await service.CreateAsync(request, cancellationToken);
        return Created($"/api/suppliers/{supplier.Id}", supplier);
    }
}
