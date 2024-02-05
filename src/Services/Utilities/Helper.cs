using System.Net;
using AzureSidekick.Core.Exceptions;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.OperationResults;

namespace AzureSidekick.Services.Utilities;

internal static class Helper
{
    /// <summary>
    /// Get the failure operation result from exception.
    /// </summary>
    /// <param name="exception">
    /// <see cref="Exception"/>.
    /// </param>
    /// <param name="logger">
    /// <see cref="ILogger"/>.
    /// </param>
    /// <param name="context">
    /// <see cref="IOperationContext"/>.
    /// </param>
    /// <returns>
    /// <see cref="FailOperationResult"/>.
    /// </returns>
    internal static IOperationResult GetFailOperationResultFromException(Exception exception, ILogger logger,
        IOperationContext context)
    {
        var requestException = exception as RequestException;
        if (requestException == null) //Log exception only if it has not been logged already.
        {
            logger?.LogException(exception, context);
        }

        IOperationResult operationResult = new FailOperationResult()
        {
            OperationId = context.OperationId,
            Error = exception,
            StatusCode = requestException?.StatusCode ?? HttpStatusCode.InternalServerError
        };
        return operationResult;
    }
}