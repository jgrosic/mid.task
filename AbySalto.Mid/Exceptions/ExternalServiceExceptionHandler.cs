using AbySalto.Mid.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace AbySalto.Mid.WebApi.Exceptions;

public sealed class ExternalServiceExceptionHandler(ILogger<ExternalServiceExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ExternalServiceException)
            return false;

        logger.LogError(exception, "External service request failed.");

        await Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Bad Gateway",
                detail: "An external service request failed.")
            .ExecuteAsync(httpContext);

        return true;
    }
}