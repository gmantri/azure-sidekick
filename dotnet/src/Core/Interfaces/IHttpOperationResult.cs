using System.Net;

namespace AzureSidekick.Core.Interfaces;

/// <summary>
/// Interface for any operation result that performs an HTTP request.
/// </summary>
public interface IHttpOperationResult : IOperationResult
{
    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    HttpStatusCode StatusCode { get; set; }
}