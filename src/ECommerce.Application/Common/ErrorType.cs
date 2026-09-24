namespace ECommerce.Application.Common;

/// <summary>
/// Categorizes the nature of an error so the presentation layer
/// can map it to the correct HTTP status code.
/// </summary>
public enum ErrorType
{
    /// <summary>General failure (HTTP 500 / 400 depending on context).</summary>
    Failure = 0,

    /// <summary>Business rule or input validation failure (HTTP 400).</summary>
    Validation = 1,

    /// <summary>Requested resource was not found (HTTP 404).</summary>
    NotFound = 2,

    /// <summary>State conflict, e.g., duplicate slug or deleting an entity in use (HTTP 409).</summary>
    Conflict = 3,

    /// <summary>Authentication or permission failure (HTTP 401 / 403).</summary>
    Unauthorized = 4,

    /// <summary>Upstream service failure (HTTP 502).</summary>
    BadGateway = 5
}
