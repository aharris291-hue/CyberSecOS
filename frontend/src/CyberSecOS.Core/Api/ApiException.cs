using System.Net;

namespace CyberSecOS.Core.Api;

/// <summary>Raised when the backend rejects a request or cannot be reached.</summary>
public sealed class ApiException : Exception
{
    public ApiException(HttpStatusCode? statusCode, string message, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }

    /// <summary>Null when the request never got a response (backend offline, timeout).</summary>
    public HttpStatusCode? StatusCode { get; }

    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
}
