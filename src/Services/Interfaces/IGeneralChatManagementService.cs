using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Core.OperationResults;

namespace AzureSidekick.Services.Interfaces;

/// <summary>
/// Interface for general chat operations.
/// </summary>
public interface IGeneralChatManagementService : IChatManagementService
{
    /// <summary>
    /// Rephrases a question.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    Task<IOperationResult> Rephrase(string question, IOperationContext operationContext = default);

    /// <summary>
    /// Gets the intent of a question.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    Task<IOperationResult> GetIntent(string question, IOperationContext operationContext = default);

    /// <summary>
    /// Get the response for a question based on the question's intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="intent">
    /// Question's intent.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    Task<IOperationResult> GetResponse(string question, string intent,
        IOperationContext operationContext = default);
    
    /// <summary>
    /// Get a streaming response for a question based on the question's intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="intent">
    /// Question's intent.
    /// </param>
    /// <param name="state">
    /// <see cref="StreamingResponseState"/>.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    Task GetStreamingResponse(string question, string intent,
        StreamingResponseState state = default, IOperationContext operationContext = default);

    /// <summary>
    /// Clear chat history.
    /// </summary>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    Task<IOperationResult> ClearChatHistory(IOperationContext operationContext = default);
}