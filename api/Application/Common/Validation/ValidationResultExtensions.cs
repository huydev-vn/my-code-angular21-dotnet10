using Application.Common.Errors;
using Application.Common.Results;
using FluentValidation;
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

        var message = string.Join(
            " ",
            validation.Errors.Select(error => error.ErrorMessage));

        return Error.Validation("identity.validation", message);
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
