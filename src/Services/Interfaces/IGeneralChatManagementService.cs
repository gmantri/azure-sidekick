using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Core.OperationResults;

namespace AzureSidekick.Services.Interfaces;

/// <summary>
/// Interface for general chat operations.
/// </summary>
public interface IGeneralChatManagementService
{
    /// <summary>
    /// Rephrases a question.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    Task<IOperationResult> Rephrase(string question, IEnumerable<ChatResponse> chatHistory, IOperationContext operationContext = default);

    /// <summary>
    /// Gets the intent of a question.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    Task<IOperationResult> GetIntent(string question, IEnumerable<ChatResponse> chatHistory, IOperationContext operationContext = default);

    /// <summary>
    /// Handles question with Azure intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    Task<IOperationResult> HandleQuestionWithAzureIntent(string question, IEnumerable<ChatResponse> chatHistory, IOperationContext operationContext = default);

    /// <summary>
    /// Handles question with multiple intents.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    Task<IOperationResult> HandleQuestionWithMultipleIntents(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default);

    /// <summary>
    /// Handles question with information intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    Task<IOperationResult> HandleQuestionWithInformationIntent(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default);

    /// <summary>
    /// Handles question with ability intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    Task<IOperationResult> HandleQuestionWithAbilityIntent(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default);

    /// <summary>
    /// Handles question with unclear intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    Task<IOperationResult> HandleQuestionWithUnclearIntent(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default);

    /// <summary>
    /// Handles question with other intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    Task<IOperationResult> HandleQuestionWithOtherIntent(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default);
}