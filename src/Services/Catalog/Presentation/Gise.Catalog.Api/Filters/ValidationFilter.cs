using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Gise.Catalog.Api.Filters;

// Action'a gelen her argüman için kayıtlı bir validator varsa çalıştırır; handler'a geçersiz komut ulaşmaz.
public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (result.IsValid)
                continue;

            var errors = result.Errors
                .GroupBy(e => ToCamelCasePath(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors)
            {
                Title = "Bazı bilgiler hatalı.",
                Detail = "Aşağıdaki alanları düzeltip tekrar deneyin.",
                Status = StatusCodes.Status400BadRequest
            });
            return;
        }

        await next();
    }

    // "Sections[0].Rows[1].SeatCount" → "sections[0].rows[1].seatCount": istemcideki alan adlarıyla aynı.
    private static string ToCamelCasePath(string path)
        => string.Join('.', path.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
