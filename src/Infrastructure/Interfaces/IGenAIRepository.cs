using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;

namespace AzureSidekick.Infrastructure.Interfaces;

/// <summary>
/// Interface for Generative AI operations.
/// </summary>
public interface IGenAIRepository
{
    // /// <summary>
    // /// Recognizes the intent of a prompt.
    // /// </summary>
    // /// <param name="prompt">
    // /// User question.
    // /// </param>
    // /// <param name="chatHistory">
    // /// Chat history.
    // /// </param>
    // /// <param name="operationContext">
    // /// Operation context.
    // /// </param>
    // /// <returns>
    // /// <see cref="ChatResponse"/>.
    // /// </returns>
    // Task<ChatResponse> RecognizeIntent(string prompt, IEnumerable<ChatResponse> chatHistory, IOperationContext operationContext = default);
    //
    // /// <summary>
    // /// Rephrases a prompt so that it is clear.
    // /// </summary>
    // /// <param name="prompt">
    // /// User question.
    // /// </param>
    // /// <param name="chatHistory">
    // /// Chat history.
    // /// </param>
    // /// <param name="operationContext">
    // /// Operation context.
    // /// </param>
    // /// <returns>
    // /// <see cref="ChatResponse"/>.
    // /// </returns>
    // Task<ChatResponse> Rephrase(string prompt, IEnumerable<ChatResponse> chatHistory, IOperationContext operationContext = default);
    //
    // /// <summary>
    // /// Executes a prompt.
    // /// </summary>
    // /// <param name="prompt">
    // /// User question.
    // /// </param>
    // /// <param name="intent">
    // /// User question's intent.
    // /// </param>
    // /// <param name="chatHistory">
    // /// Chat history.
    // /// </param>
    // /// <param name="arguments">
    // /// Arguments for prompt execution. It will contain the data that will be passed to the prompt template.
    // /// </param>
    // /// <param name="operationContext">
    // /// Operation context.
    // /// </param>
    // /// <returns>
    // /// <see cref="ChatResponse"/>.
    // /// </returns>
    // Task<ChatResponse> ExecutePrompt(string prompt, string intent, IEnumerable<ChatResponse> chatHistory, IDictionary<string, object> arguments = default, IOperationContext operationContext = default);

    /// <summary>
    /// Generate response to a user's question.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="pluginName">
    /// Name of the plugin.
    /// </param>
    /// <param name="functionName">
    /// Name of the function.
    /// </param>
    /// <param name="arguments">
    /// Arguments for prompt execution. It will contain the data that will be passed to the prompt template.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// <see cref="ChatResponse"/>.
    /// </returns>
    Task<ChatResponse> GenerateResponse(string question, string pluginName, string functionName, IDictionary<string, object> arguments = default, IOperationContext operationContext = default);
}