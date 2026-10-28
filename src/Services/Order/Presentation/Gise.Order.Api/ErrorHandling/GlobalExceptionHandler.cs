using Gise.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Gise.Order.Api.ErrorHandling;

// Merkezi hataları (Gise.Exceptions) ProblemDetails'e çevirir: title = Message, detail = Description,
// errorCode = ErrorCode. Tanınmayan her hata 500 olur ve loglanır.
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
            ValidationException validation => new ValidationProblemDetails(new Dictionary<string, string[]>(validation.Errors)),
            AppException => new ProblemDetails(),
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Beklenmeyen bir hata oluştu."
            }
        };

        if (exception is AppException app)
        {
            problem.Status = app.StatusCode;
            problem.Title = app.Message;
            problem.Detail = app.Description;
        }
        else
        {
            _logger.LogError(exception, "İşlenmeyen hata.");
        }

        problem.Extensions["errorCode"] = (exception as AppException)?.ErrorCode ?? ErrorCodes.General.InternalError;

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }
}
