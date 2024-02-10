using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Infrastructure.Interfaces;

namespace AzureSidekick.Infrastructure.Repository;

/// <summary>
/// Implementation of <see cref="IChatHistoryOperationsRepository"/> where chat history items are kept in memory.
/// </summary>
public class InMemoryChatHistoryOperationsRepository : IChatHistoryOperationsRepository
{
    /// <summary>
    /// Chat responses.
    /// </summary>
    private readonly List<ChatResponse> _chatResponses;
    
    /// <summary>
    /// Create an instance of <see cref="InMemoryChatHistoryOperationsRepository"/>.
    /// </summary>
    public InMemoryChatHistoryOperationsRepository()
    {
        _chatResponses = new List<ChatResponse>();
    }
    
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
    public async Task<IReadOnlyList<ChatResponse>> List(string key = default, IOperationContext operationContext = default)
    {
        return await Task.FromResult(_chatResponses.AsReadOnly());
    }

    /// <summary>
    /// Clears the chat history.
    /// </summary>
    /// <param name="key">
    /// Lookup key.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    public async Task Clear(string key = default, IOperationContext operationContext = default)
    {
        await Task.Run(() =>
        {
            _chatResponses.Clear(); 
        });
    }

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
    public async Task Add(ChatResponse chatResponse, string key = default, IOperationContext operationContext = default)
    {
        await Task.Run(() =>
        {
            _chatResponses.Add(chatResponse);
        });
    }
}