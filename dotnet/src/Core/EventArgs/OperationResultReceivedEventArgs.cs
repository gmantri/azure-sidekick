using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;

namespace AzureSidekick.Core.EventArgs;

public class OperationResultReceivedEventArgs : System.EventArgs
{
    /// <summary>
    /// Gets or sets <see cref="IOperationResult"/>.
    /// </summary>
    public IOperationResult OperationResult { get; set; }
    
    /// <summary>
    /// Indicates that <see cref="OperationResult"/> is the last response.
    /// </summary>
    public bool IsLastResponse { get; set; }
    
    /// <summary>
    /// <see cref="StreamingResponseState"/>.
    /// </summary>
    public StreamingResponseState State { get; set; }
}