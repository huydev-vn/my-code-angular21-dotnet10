using System.Diagnostics;
using Application.Common.Errors;
using Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Api.Extensions;

internal static class ResultExtensions
{
    public static ActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return result.Error!.ToActionResult();
    }

    public static ActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return result.Error!.ToActionResult();
    }

    public static ActionResult ToActionResult(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Type.ToString(),
            Detail = error.Message,
            Type = error.Type switch
            {
                ErrorType.Validation => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                ErrorType.Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                ErrorType.NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                ErrorType.Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            }
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = Activity.Current?.Id;

        return new ObjectResult(problem)
        {
            StatusCode = statusCode
        };
    }
}
