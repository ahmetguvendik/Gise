using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gise.HealthCheck.Api.Checks;

// Bileşen bağlantı kabul ediyor mu (Kafka, RabbitMQ, Redis, PostgreSQL). Kimlik bilgisi gerekmez: altyapı hesapları panele
// verilmez. Protokol doğrulanmaz; "kapalı mı açık mı" sorusunun cevabı bu. Servislerin kendi /health'leri (veritabanı
// sorgusu, nabız) daha derin kontrol yapıyor.
internal sealed class TcpHealthCheck : IHealthCheck
{
    private readonly string _host;
    private readonly int _port;
    private readonly TimeSpan _timeout;

    public TcpHealthCheck(string target, TimeSpan timeout)
    {
        var parts = target.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out _port))
            throw new InvalidOperationException($"TCP hedefi 'ad:port' biçiminde olmalı: '{target}'.");

        _host = parts[0];
        _timeout = timeout;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port, timeout.Token);
            return HealthCheckResult.Healthy($"{_host}:{_port} bağlantı kabul ediyor.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Fail($"{_host}:{_port} {_timeout.TotalSeconds:0} sn içinde yanıt vermedi.", context);
        }
        catch (SocketException ex)
        {
            return Fail($"{_host}:{_port} bağlantı kurulamadı: {ex.SocketErrorCode}.", context);
        }
    }

    private static HealthCheckResult Fail(string description, HealthCheckContext context)
        => new(context.Registration.FailureStatus, description);
}
