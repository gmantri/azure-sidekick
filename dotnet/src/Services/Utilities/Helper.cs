using System.Net;
using AzureSidekick.Core.Exceptions;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Core.OperationResults;
using AzureSidekick.Infrastructure.Interfaces;

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

    /// <summary>
    /// Add chat response to chat history.
    /// </summary>
    /// <param name="repository">
    /// <see cref="IChatHistoryOperationsRepository"/>.
    /// </param>
    /// <param name="chatResponse">
    /// <see cref="ChatResponse"/>.
    /// </param>
    /// <param name="operationContext">
    /// <see cref="IOperationContext"/>.
    /// </param>
    internal static async Task SaveChatResponse(IChatHistoryOperationsRepository repository, ChatResponse chatResponse, IOperationContext operationContext)
    {
        if (!chatResponse.StoreInChatHistory) return;
        await repository.Add(chatResponse, operationContext.UserId, operationContext);
    }
}