using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using Gise.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
// DataAnnotations'ın ValidationException'ı ile isim çakışıyor; burada her zaman merkezi olan kastediliyor.
using ValidationException = Gise.Exceptions.ValidationException;

namespace Gise.Reservation.Api.ErrorHandling;

// [ApiController]'ın model bağlama aşamasındaki 400'leri (istek bizim doğrulamamıza gelmeden: yanlış tip, bozuk JSON, boş
// gövde, eksik zorunlu alan) diğer hatalarla aynı biçime çevirir (Telgraf'taki InvalidModelStateResponseFactory): Türkçe
// başlık, errorCode VALIDATION_FAILED, traceId. Alan mesajları Türkçe; .NET tip adları ve JSON ayrıntıları (satır, sütun)
// yanıta yazılmaz. Alan adları ön yüzdeki gibi: "Sections[0].Name" ve "$.sections[0].name" → "sections[0].name".
// Her API'de aynı kopya (Catalog, Identity, Order, Reservation, Search, Ticket); biri değişirse hepsi değişir.
public static class ModelValidationResponses
{
    private const string Required = "Bu alan zorunlu.";
    private const string InvalidValue = "Geçersiz değer.";
    private const string NotANumber = "Sayı olmalı.";
    private const string BodyUnreadable = "İstek gövdesi eksik ya da okunamadı.";

    public static IMvcBuilder AddTurkishModelValidation(this IMvcBuilder builder)
    {
        builder.AddMvcOptions(options =>
        {
            // Model bağlamanın kendi mesajları (yanlış tip, eksik değer).
            var messages = options.ModelBindingMessageProvider;
            messages.SetAttemptedValueIsInvalidAccessor((_, _) => InvalidValue);
            messages.SetMissingBindRequiredValueAccessor(_ => Required);
            messages.SetMissingKeyOrValueAccessor(() => Required);
            messages.SetMissingRequestBodyRequiredValueAccessor(() => BodyUnreadable);
            messages.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => InvalidValue);
            messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => InvalidValue);
            messages.SetNonPropertyValueMustBeANumberAccessor(() => NotANumber);
            messages.SetUnknownValueIsInvalidAccessor(_ => InvalidValue);
            messages.SetValueIsInvalidAccessor(_ => InvalidValue);
            messages.SetValueMustBeANumberAccessor(_ => NotANumber);
            messages.SetValueMustNotBeNullAccessor(_ => Required);

            // Boş olamayan (string? olmayan) özellikler ASP.NET'te örtük olarak zorunludur; mesajı DataAnnotations'tan gelir
            // ("The Name field is required."). Bu sağlayıcı DataAnnotations'tan sonra çalışır ve mesajı Türkçe yapar.
            options.ModelMetadataDetailsProviders.Add(new RequiredMessageProvider());
        });

        // JSON okunamazsa ayrıntısı (tip adı, satır/sütun) mesaja girmez; boş mesaj aşağıda "Geçersiz değer." olur.
        builder.AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false);

        builder.ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = CreateResponse);
        return builder;
    }

    private static IActionResult CreateResponse(ActionContext context)
    {
        // Gövde parametresi (ör. "command"): gövde hiç okunamayınca ASP.NET onu da "zorunlu" sayar; genel hata olarak yazılır.
        var bodyParameters = context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var errors = new Dictionary<string, List<string>>();
        foreach (var (key, entry) in context.ModelState)
        {
            if (entry.Errors.Count == 0)
                continue;

            var field = bodyParameters.Contains(key) ? string.Empty : FieldName(key);
            foreach (var error in entry.Errors)
            {
                var message = field.Length == 0
                    ? BodyUnreadable
                    : string.IsNullOrWhiteSpace(error.ErrorMessage) || error.Exception is not null ? InvalidValue : error.ErrorMessage;

                var list = errors.TryGetValue(field, out var existing) ? existing : errors[field] = [];
                if (!list.Contains(message))
                    list.Add(message);
            }
        }

        // Alan hatası varsa genel "gövde okunamadı" tekrar edilmez.
        if (errors.Count > 1)
            errors.Remove(string.Empty);

        // Başlık ve açıklama bizim doğrulama hatasıyla aynı (ValidationException).
        var template = new ValidationException(new Dictionary<string, string[]>());
        var problem = new ValidationProblemDetails(errors.ToDictionary(e => e.Key, e => e.Value.ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = template.Message,
            Detail = template.Description,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        };
        problem.Extensions["errorCode"] = template.ErrorCode;
        problem.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }

    // "$.sections[0].name", "Sections[0].Name" → "sections[0].name"; "$" (gövdenin tamamı) → "".
    private static string FieldName(string key)
    {
        var path = key.StartsWith('$') ? key.TrimStart('$').TrimStart('.') : key;
        return string.Join('.', path.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(JsonNamingPolicy.CamelCase.ConvertName));
    }

    private sealed class RequiredMessageProvider : IValidationMetadataProvider
    {
        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            foreach (var required in context.ValidationMetadata.ValidatorMetadata.OfType<RequiredAttribute>())
            {
                if (required.ErrorMessage is null && required.ErrorMessageResourceName is null)
                    required.ErrorMessage = Required;
            }
        }
    }
}
