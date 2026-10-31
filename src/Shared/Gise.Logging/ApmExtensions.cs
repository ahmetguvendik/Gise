using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gise.Logging;

public static class ApmExtensions
{
    // Elastic APM ajanı: istekler, veritabanı ve HTTP/gRPC çağrıları kendiliğinden izlenir (bkz. tasarım 8). Varsayılanlar en
    // düşük öncelikle eklenir; appsettings ya da ortam değişkeni (ElasticApm__...) ezebilir.
    public static void UseAppApm(this IHostApplicationBuilder builder, string serviceName)
    {
        builder.Configuration.Sources.Insert(0, new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["ElasticApm:ServiceName"] = serviceName,
                ["ElasticApm:ServerUrl"] = "http://apm-server:8200",
                ["ElasticApm:Environment"] = builder.Environment.EnvironmentName,

                // Bulut ortamı yok; ajan açılışta AWS, Azure ve GCP meta veri uçlarını yoklamasın.
                ["ElasticApm:CloudProvider"] = "none",

                // Sağlık yoklamaları her birkaç saniyede bir geliyor; kaydedilseler işlemlerin çoğunu oluştururlardı.
                ["ElasticApm:TransactionIgnoreUrls"] = "/health*",

                // Ajanın varsayılan listesi + *cookie*: Cookie başlığı açık metin kaydediliyordu (Telgraf dersi). Liste
                // değiştirilirse varsayılanlar da yazılmalı, yoksa kaybolurlar.
                ["ElasticApm:SanitizeFieldNames"] =
                    "password, passwd, pwd, secret, *key, *token*, *session*, *credit*, *card*, *auth*, *cookie*, *principal*"
            }
        });

        builder.Services.AddAllElasticApm();
    }
}
