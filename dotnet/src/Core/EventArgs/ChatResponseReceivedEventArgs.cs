using AzureSidekick.Core.Models;

namespace AzureSidekick.Core.EventArgs;

/// <summary>
/// Event data for streaming chat response. 
/// </summary>
public class ChatResponseReceivedEventArgs : System.EventArgs
{
    /// <summary>
    /// Gets or sets <see cref="ChatResponse"/>.
    /// </summary>
    public ChatResponse ChatResponse { get; set; }
    
    /// <summary>
    /// Indicates that <see cref="ChatResponse"/> is the last response.
    /// </summary>
    public bool IsLastResponse { get; set; }
    
    /// <summary>
    /// <see cref="StreamingResponseState"/>.
    /// </summary>
    public StreamingResponseState State { get; set; }
}