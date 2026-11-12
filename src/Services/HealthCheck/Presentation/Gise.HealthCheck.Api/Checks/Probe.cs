namespace Gise.HealthCheck.Api.Checks;

internal static class Probe
{
    public const string HttpClientName = "probe";

    // Açıklamada adres yeterli; şema ve sorgu dizesi gürültü.
    public static string Display(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? $"{uri.Host}:{uri.Port}{uri.AbsolutePath}" : url;

    public static string Reason(Exception ex)
        => ex is TaskCanceledException ? "zaman aşımı" : ex.InnerException?.Message ?? ex.Message;
}
