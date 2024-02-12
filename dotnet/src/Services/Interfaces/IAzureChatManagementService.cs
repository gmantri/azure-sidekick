using Azure.Core;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.OperationResults;

namespace AzureSidekick.Services.Interfaces;

/// <summary>
/// Interface to Azure related chat operations.
/// </summary>
public interface IAzureChatManagementService
{
    /// <summary>
    /// Azure service.
    /// </summary>
    string ServiceName { get; }
    
    /// <summary>
    /// Get a response to user's question.
    /// </summary>
    /// <param name="subscriptionId">
    /// Azure subscription id.
    /// </param>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="credential">
    /// <see cref="TokenCredential"/>.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    Task<IOperationResult> GetResponse(string subscriptionId, string question, TokenCredential credential = default, IOperationContext operationContext = default);

    /// <summary>
    /// Get a streaming response to user's question.
    /// </summary>
    /// <param name="subscriptionId">
    /// Azure subscription id.
    /// </param>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="credential">
    /// <see cref="TokenCredential"/>.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    IAsyncEnumerable<IOperationResult> GetStreamingResponse(string subscriptionId, string question,
        TokenCredential credential = default,
        IOperationContext operationContext = default);
}