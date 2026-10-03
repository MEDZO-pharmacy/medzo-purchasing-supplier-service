using Medzo.PurchasingSupplier.Application.Suppliers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Medzo.PurchasingSupplier.Api.ExceptionHandling;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, errors) = exception switch
        {
            SupplierValidationException validation => (StatusCodes.Status400BadRequest, "Validation failed", validation.Errors),
            SupplierDuplicateException => (StatusCodes.Status409Conflict, "Possible duplicate supplier", (IReadOnlyDictionary<string, string[]>?)null),
            SupplierNotFoundException => (StatusCodes.Status404NotFound, "Supplier not found", (IReadOnlyDictionary<string, string[]>?)null),
            SupplierMustBeInactiveException => (StatusCodes.Status409Conflict, "Supplier must be inactive", (IReadOnlyDictionary<string, string[]>?)null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
        };
        if (status == 500) logger.LogError(exception, "Unhandled request error.");
        context.Response.StatusCode = status;
        return problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = status == 500 ? null : exception.Message, Extensions = { ["errors"] = errors } }
        });
    }
}
