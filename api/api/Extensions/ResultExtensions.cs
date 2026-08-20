using Application.Common.Errors;
using Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Api.Extensions;

internal static class ResultExtensions
{
    public static ActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return result.Error!.ToActionResult(controller.HttpContext);
    }

    public static ActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return result.Error!.ToActionResult(controller.HttpContext);
    }

    public static ActionResult ToCreatedAtAction<T>(
        this Result<T> result,
        ControllerBase controller,
        string actionName,
        object routeValues)
    {
        if (result.IsSuccess)
        {
            return controller.CreatedAtAction(actionName, routeValues, result.Value);
        }

        return result.Error!.ToActionResult(controller.HttpContext);
    }

    private static ActionResult ToActionResult(this Error error, HttpContext httpContext)
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

        ProblemDetails problem;
        if (error.Details is { Count: > 0 })
        {
            var validation = new ValidationProblemDetails
            {
                Status = statusCode,
                Title = "Validation failed",
                Detail = error.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Instance = httpContext.Request.Path
            };
            foreach (var (key, messages) in error.Details)
            {
                validation.Errors[key] = messages;
            }

            problem = validation;
        }
        else
        {
            problem = new ProblemDetails
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
                },
                Instance = httpContext.Request.Path
            };
        }

        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return new ObjectResult(problem)
        {
            StatusCode = statusCode
        };
    }
}
