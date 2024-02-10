namespace AzureSidekick.Core.Interfaces;

/// <summary>
/// Interface for operation result.
/// </summary>
public interface IOperationResult
{
    /// <summary>
    /// Gets or sets the operation id.
    /// </summary>
    string OperationId { get; set; }

    /// <summary>
    /// Indicates if the operation was successful or not.
    /// </summary>
    bool IsOperationSuccessful { get; }

    /// <summary>
    /// Gets or sets the error.
    /// </summary>
    Exception Error { get; set; }
}