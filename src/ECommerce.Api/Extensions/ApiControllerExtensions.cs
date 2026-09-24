using ECommerce.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Extensions;

/// <summary>
/// Extensions to map Application Result objects to HTTP action results.
/// </summary>
public static class ApiControllerExtensions
{
    /// <summary>
    /// Translates a Result (without value) into the corresponding IActionResult.
    /// If successful, returns 204 No Content.
    /// </summary>
    public static IActionResult Match(this ControllerBase controller, Result result)
    {
        return result.IsSuccess 
            ? controller.NoContent()
            : CreateProblemDetails(controller, result.Error);
    }

    /// <summary>
    /// Translates a Result&lt;T&gt; into the corresponding IActionResult.
    /// If successful, returns 200 OK with the value.
    /// </summary>
    public static IActionResult Match<T>(this ControllerBase controller, Result<T> result)
    {
        return result.IsSuccess 
            ? controller.Ok(result.Value)
            : CreateProblemDetails(controller, result.Error);
    }

    /// <summary>
    /// Translates a Result&lt;T&gt; into the corresponding IActionResult, 
    /// allowing a custom success mapping (e.g., CreatedAtAction).
    /// </summary>
    public static IActionResult Match<T>(
        this ControllerBase controller, 
        Result<T> result, 
        Func<T, IActionResult> onSuccess)
    {
        return result.IsSuccess 
            ? onSuccess(result.Value)
            : CreateProblemDetails(controller, result.Error);
    }

    private static IActionResult CreateProblemDetails(ControllerBase controller, Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.BadGateway => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Type.ToString(),
            Detail = error.Message,
            Type = $"https://httpstatuses.io/{statusCode}"
        };

        problemDetails.Extensions["code"] = error.Code;

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
