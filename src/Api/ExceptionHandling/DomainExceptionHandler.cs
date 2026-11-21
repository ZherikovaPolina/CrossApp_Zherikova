using Core;
using Microsoft.AspNetCore.Diagnostics;

namespace Api.ExceptionHandling;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException)
            return false;

        httpContext.Response.StatusCode =
            StatusCodes.Status409Conflict;

        await Results.Problem(
            title: "Порушення бізнес-правила",
            detail: "Операцію неможливо виконати через порушення правил замовлення.",
            statusCode: StatusCodes.Status409Conflict
        ).ExecuteAsync(httpContext);

        return true;
    }
}
