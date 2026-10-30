using System.Text.Json;
using FluentValidation;
using MediatR;
// FluentValidation'ın kendi ValidationException'ı ile isim çakışıyor; burada her zaman merkezi olan kastediliyor.
using ValidationException = Gise.Exceptions.ValidationException;

namespace Gise.Search.Application.Behaviors;

// Handler'a ulaşmadan önce ilgili command/query için kayıtlı validator'ları çalıştırır.
// Hata varsa ValidationException fırlatır; GlobalExceptionHandler bunu 400'e çevirir.
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .ToList();

        if (failures.Count > 0)
        {
            var errors = failures
                .GroupBy(failure => ToCamelCasePath(failure.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(failure => failure.ErrorMessage).ToArray());

            throw new ValidationException(errors);
        }

        return await next(cancellationToken);
    }

    // "Sections[0].Rows[1].SeatCount" → "sections[0].rows[1].seatCount": istemcideki alan adlarıyla aynı.
    private static string ToCamelCasePath(string path)
        => string.Join('.', path.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
