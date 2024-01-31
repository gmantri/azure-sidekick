using System.Net;
using AzureSidekick.Core.Interfaces;

namespace AzureSidekick.Core.OperationResults;

/// <summary>
/// Class for storing failure result of an operation.
/// </summary>
public class FailOperationResult : IHttpOperationResult
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
    public bool IsOperationSuccessful => false;

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }
}