using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Gise.Authentication;

// Bütün API'ler token'ı aynı şekilde doğrular. Ayarlar appsettings'teki Keycloak bölümünden okunur; adresler compose'dan
// ortam değişkeniyle gelir.
public static class KeycloakAuthentication
{
    public static AuthorizationBuilder AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var keycloak = configuration.GetSection("Keycloak");
                string Required(string key) => keycloak[key] ?? throw new InvalidOperationException($"Keycloak:{key} ayarı eksik.");

                // Anahtarlar konteyner içinden (keycloak:8080) alınır; token'ın issuer'ı ise tarayıcının
                // gördüğü adrestir (localhost:8180). İkisi de geçerli sayılır.
                options.MetadataAddress = $"{Required("InternalAuthority")}/.well-known/openid-configuration";
                options.RequireHttpsMetadata = keycloak.GetValue("RequireHttpsMetadata", true);
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuers = [Required("InternalAuthority"), Required("PublicAuthority")],
                    ValidAudience = Required("Audience"),
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };
            });

        // API kendi policy'lerini dönen builder'a ekler (her API'nin Authentication/Policies.cs dosyası).
        return services.AddAuthorizationBuilder();
    }
}
