using System.Net;
using AzureSidekick.Core.Interfaces;

namespace AzureSidekick.Core.OperationResults;

/// <summary>
/// Class for storing success result of an operation.
/// </summary>
/// <typeparam name="T">
/// Type of object returned.
/// </typeparam>
public sealed class SuccessOperationResult<T> : IHttpOperationResult
{
    /// <summary>
    /// Gets or sets the error.
    /// </summary>
    public Exception Error { get; set; }

    /// <summary>
    /// Gets or sets the operation id for the database operation.
    /// </summary>
    public string OperationId { get; set; }

    /// <summary>
    /// Indicates if the operation is successful or not.
    /// </summary>
    public bool IsOperationSuccessful => true;

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }
    
    /// <summary>
    /// Operation result when the operation returns a single item.
    /// </summary>
    public T Item { get; set; }
    
    /// <summary>
    /// Operation result when the operation returns multiple items.
    /// </summary>
    public IEnumerable<T> Items { get; set; }
}