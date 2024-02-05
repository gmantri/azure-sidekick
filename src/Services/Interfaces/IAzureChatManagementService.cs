using Azure.Core;
using AzureSidekick.Core;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Core.OperationResults;

namespace AzureSidekick.Services.Interfaces;

/// <summary>
/// Interface to Azure related chat operations.
/// </summary>
public interface IAzureChatManagementService : IChatManagementService
{
    /// <summary>
    /// Azure service. Could be one of the values from <see cref="Constants.ServiceIntents"/>.
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
    /// <param name="chatHistory">
    /// Chat history.
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
    Task<IOperationResult> GetResponse(string subscriptionId, string question, IEnumerable<ChatResponse> chatHistory, TokenCredential credential = default, IOperationContext operationContext = default);
    
    /// <summary>
    /// Get a streaming response to user's question.
    /// </summary>
    /// <param name="subscriptionId">
    /// Azure subscription id.
    /// </param>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="state">
    /// <see cref="StreamingResponseState"/>.
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
    Task GetStreamingResponse(string subscriptionId, string question, IEnumerable<ChatResponse> chatHistory, StreamingResponseState state = default, TokenCredential credential = default, IOperationContext operationContext = default);
}