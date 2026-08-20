using Application.Common.Errors;
using Application.Common.Results;
using FluentValidation.Results;

namespace Application.Common.Validation;

internal static class ValidationResultExtensions
{
    public static Error? ToError(this ValidationResult validation)
    {
        if (validation.IsValid)
        {
            return null;
        }

        var details = validation.Errors
            .GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName)
                ? "request"
                : error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

        return Error.Validation(
            "validation.failed",
            "One or more validation errors occurred.",
            details);
    }

    public static Result<T>? ToFailure<T>(this ValidationResult validation)
    {
        var error = validation.ToError();
        return error is null ? null : Result<T>.Failure(error);
    }

    public static Result? ToFailure(this ValidationResult validation)
    {
        var error = validation.ToError();
        return error is null ? null : Result.Failure(error);
    }
}
