using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;

namespace AzureSidekick.Infrastructure.Interfaces;

/// <summary>
/// Interface for Generative AI operations.
/// </summary>
public interface IGenAIRepository
{
    /// <summary>
    /// Get response to a user's question.
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
    Task<ChatResponse> GetResponse(string question, string pluginName, string functionName, IDictionary<string, object> arguments = default, IOperationContext operationContext = default);
    
    /// <summary>
    /// Get streaming response to a user's question.
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
    /// <see cref="IAsyncEnumerable{ChatResponse}"/>.
    /// </returns>
    IAsyncEnumerable<ChatResponse> GetStreamingResponse(string question, string pluginName, string functionName, IDictionary<string, object> arguments = default, IOperationContext operationContext = default);
}