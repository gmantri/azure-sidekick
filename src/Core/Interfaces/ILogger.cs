using AzureSidekick.Core.Models;

namespace AzureSidekick.Core.Interfaces;

/// <summary>
/// Interface for logging operations and exceptions.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs an operation.
    /// </summary>
    /// <param name="operationContext">
    /// <see cref="IOperationContext"/>.
    /// </param>
    void LogOperation(IOperationContext operationContext);

    /// <summary>
    /// Logs an exception.
    /// </summary>
    /// <param name="exception">
    /// <see cref="Exception"/>.
    /// </param>
    /// <param name="operationContext">
    /// <see cref="IOperationContext"/>.
    /// </param>
    void LogException(Exception exception, IOperationContext operationContext);

    /// <summary>
    /// Logs the chat response.
    /// </summary>
    /// <param name="response">
    /// <see cref="ChatResponse"/>.
    /// </param>
    /// <param name="operationContext">
    /// <see cref="IOperationContext"/>.
    /// </param>
    void LogChatResponse(ChatResponse response, IOperationContext operationContext);
}