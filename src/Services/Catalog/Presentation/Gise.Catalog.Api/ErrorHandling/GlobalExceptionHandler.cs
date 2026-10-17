using System.Text.Json;
using FluentValidation;
using Gise.Catalog.Application.Exceptions;
using Gise.Catalog.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Catalog.Api.ErrorHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validation => ToValidationProblem(validation),
            ForbiddenException => new ProblemDetails { Status = StatusCodes.Status403Forbidden, Title = exception.Message },
            NotFoundException => new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = exception.Message },
            DomainException => new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = exception.Message },
            _ => new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "Beklenmeyen bir hata oluştu." }
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "İşlenmeyen hata.");

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }

    private static ValidationProblemDetails ToValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => ToCamelCasePath(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Bazı bilgiler hatalı.",
            Detail = "Aşağıdaki alanları düzeltip tekrar deneyin."
        };
    }

    // "Sections[0].Rows[1].SeatCount" → "sections[0].rows[1].seatCount": istemcideki alan adlarıyla aynı.
    private static string ToCamelCasePath(string path)
        => string.Join('.', path.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
