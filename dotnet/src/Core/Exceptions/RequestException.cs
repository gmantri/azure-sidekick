using System.Net;

namespace AzureSidekick.Core.Exceptions;

/// <summary>
/// Custom exception to store any request exceptions.
/// </summary>
public class RequestException : Exception
{
    /// <summary>
    /// <see cref="HttpStatusCode"/>.
    /// </summary>
    public HttpStatusCode StatusCode { get; }
    
    /// <summary>
    /// Initialize a new instance of <see cref="RequestException"/>.
    /// </summary>
    /// <param name="message">
    /// Exception message.
    /// </param>
    /// <param name="statusCode">
    /// <see cref="HttpStatusCode"/>.
    /// </param>
    public RequestException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initialize a new instance of <see cref="RequestException"/>.
    /// </summary>
    /// <param name="message">
    /// Exception message.
    /// </param>
    /// <param name="statusCode">
    /// <see cref="HttpStatusCode"/>.
    /// </param>
    /// <param name="innerException">
    /// <see cref="Exception"/>.
    /// </param>
    public RequestException(string message, HttpStatusCode statusCode, Exception innerException) : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}