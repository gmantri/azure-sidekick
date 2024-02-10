namespace AzureSidekick.Services.Interfaces;

/// <summary>
/// Base interface for chat management service.
/// </summary>
public interface IChatManagementService
{
    /// <summary>
    /// Event handler for operation result received event.
    /// </summary>
    event EventHandler OperationResultReceivedEventHandler;
}