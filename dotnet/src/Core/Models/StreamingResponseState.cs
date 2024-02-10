using AzureSidekick.Core.Interfaces;

namespace AzureSidekick.Core.Models;

/// <summary>
/// State for streaming response.
/// </summary>
public class StreamingResponseState
{
    /// <summary>
    /// Original question asked by the user.
    /// </summary>
    public string UserInput { get; set; }
    
    /// <summary>
    /// Gets or sets the prompt tokens.
    /// </summary>
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// Gets or sets the completion tokens.
    /// </summary>
    public int CompletionTokens { get; set; }
    
    /// <summary>
    /// Gets or sets the operation context.
    /// </summary>
    public IOperationContext OperationContext { get; set; }
}