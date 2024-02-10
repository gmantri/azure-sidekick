namespace AzureSidekick.Core.Models;

/// <summary>
/// Class representing chat response.
/// </summary>
public class ChatResponse
{
    private readonly DateTime _timeStamp = DateTime.UtcNow;

    /// <summary>
    /// Chat response id.
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Original question asked by the user.
    /// </summary>
    public string OriginalQuestion { get; set; }
    
    /// <summary>
    /// Question's intent.
    /// </summary>
    public string Intent { get; set; }
    
    /// <summary>
    /// Function used to answer the question.
    /// </summary>
    public string Function { get; set; }
    
    /// <summary>
    /// User question (revised by LLM).
    /// </summary>
    public string Question { get; set; }
    
    /// <summary>
    /// Chat response.
    /// </summary>
    public string Response { get; set; }
    
    /// <summary>
    /// Number of prompt tokens.
    /// </summary>
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// Number of completion tokens.
    /// </summary>
    public int CompletionTokens { get; set; }
    
    /// <summary>
    /// Indicates if this should be stored in chat history. Not all chat responses need to be stored in the history.
    /// </summary>
    public bool StoreInChatHistory { get; set; }

    /// <summary>
    /// Gets the date/time of the chat response.
    /// </summary>
    public string Timestamp => _timeStamp.ToString("s");
}