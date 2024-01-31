using AzureSidekick.Core.Interfaces;

namespace AzureSidekick.Core.Models;

/// <summary>
/// Class indicating operation context.
/// </summary>
public class OperationContext : IOperationContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationContext"/>.
    /// </summary>
    /// <param name="operationName">
    /// Operation name.
    /// </param>
    /// <param name="message">
    /// Operation message.
    /// </param>
    public OperationContext(string operationName, string message) : this(operationName, message, string.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OperationContext"/>.
    /// </summary>
    /// <param name="operationName">
    /// Operation name.
    /// </param>
    /// <param name="message">
    /// Operation message.
    /// </param>
    /// <param name="parentOperationContext">
    /// <see cref="IOperationContext"/> which represents parent operation context.
    /// </param>
    public OperationContext(string operationName, string message, IOperationContext parentOperationContext) : this(operationName, message, parentOperationContext?.OperationId)
    {
        UserId = parentOperationContext?.UserId;
        UserName = parentOperationContext?.UserName;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="OperationContext"/>.
    /// </summary>
    /// <param name="operationName">
    /// Operation name.
    /// </param>
    /// <param name="message">
    /// Operation message.
    /// </param>
    /// <param name="parentOperationId">
    /// Parent operation id.
    /// </param>
    public OperationContext(string operationName, string message, string parentOperationId)
    {
        OperationId = Guid.NewGuid().ToString();
        OperationName = operationName;
        Message = message;
        ParentOperationId = parentOperationId;
        StartTime = DateTime.UtcNow;
        Metadata = new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets the operation id.
    /// </summary>
    public string OperationId { get; }

    /// <summary>
    /// Gets or sets the operation name.
    /// </summary>
    public string OperationName { get; private set; }

    /// <summary>
    /// Gets the parent operation id.
    /// </summary>
    public string ParentOperationId { get; }

    /// <summary>
    /// Gets or sets the operation message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets the date/time (in UTC) when the operation started.
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    /// Gets or sets the date/time (in UTC) when the operation finished.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Gets or sets the id of the user who performed the operation.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who performed the operation.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Gets the custom metadata collection associated with the operation.
    /// </summary>
    public IDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets the total time taken in milliseconds for the operation to finish.
    /// </summary>
    public double ElapsedTime => (EndTime - StartTime).TotalMilliseconds;
}