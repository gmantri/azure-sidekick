using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;

namespace AzureSidekick.Infrastructure.Interfaces;

/// <summary>
/// Interface for operations on managing chat history.
/// </summary>
public interface IChatHistoryOperationsRepository
{
    /// <summary>
    /// List items in chat history.
    /// </summary>
    /// <param name="key">
    /// Lookup key.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// <see cref="IEnumerable{ChatResponse}"/>.
    /// </returns>
    Task<IReadOnlyList<ChatResponse>> List(string key = default, IOperationContext operationContext = default);

    /// <summary>
    /// Clears the chat history.
    /// </summary>
    /// <param name="key">
    /// Lookup key.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    Task Clear(string key = default, IOperationContext operationContext = default);
    
    /// <summary>
    /// Adds a chat response to the chat history.
    /// </summary>
    /// <param name="chatResponse">
    /// <see cref="ChatResponse"/>.
    /// </param>
    /// <param name="key">
    /// Lookup key.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    Task Add(ChatResponse chatResponse, string key = default, IOperationContext operationContext = default);
}