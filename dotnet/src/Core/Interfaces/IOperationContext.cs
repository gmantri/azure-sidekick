namespace AzureSidekick.Core.Interfaces;

/// <summary>
/// Interface for operation context. Operation context is used for end-to-end tracing
/// of requests in the system.
/// </summary>
public interface IOperationContext
{
    /// <summary>
    /// Gets the operation id.
    /// </summary>
    string OperationId { get; }

    /// <summary>
    /// Gets the name of the operation.
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Gets the parent operation id.
    /// </summary>
    string ParentOperationId { get; }

    /// <summary>
    /// Gets or sets the operation message.
    /// </summary>
    string Message { get; set; }

    /// <summary>
    /// Gets the date/time (in UTC) when the operation started.
    /// </summary>
    DateTime StartTime { get; }

    /// <summary>
    /// Gets or sets the date/time (in UTC) when the operation finished.
    /// </summary>
    DateTime EndTime { get; set; }

    /// <summary>
    /// Gets or sets the id of the user who performed the operation.
    /// </summary>
    string UserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who performed the operation.
    /// </summary>
    string UserName { get; set; }

    /// <summary>
    /// Gets the total time taken in milliseconds for the operation to finish.
    /// </summary>
    double ElapsedTime { get; }

    /// <summary>
    /// Gets the custom metadata collection associated with the operation.
    /// </summary>
    IDictionary<string, object> Metadata { get; }
}