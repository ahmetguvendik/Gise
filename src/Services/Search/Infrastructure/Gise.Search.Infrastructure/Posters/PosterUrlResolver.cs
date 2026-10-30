using Gise.Search.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Gise.Search.Infrastructure.Posters;

// Catalog'un afiş adresleriyle aynı biçim: temel anahtar + "-{genişlik}.webp". Ayarlar appsettings'teki Posters
// bölümünden; Catalog'un Storage ayarlarıyla aynı olmalı.
public sealed class PosterUrlResolver : IPosterUrlResolver
{
    private readonly string _publicBase;
    private readonly int[] _variantWidths;
    private readonly int _defaultVariantWidth;

    public PosterUrlResolver(IConfiguration configuration)
    {
        var posters = configuration.GetRequiredSection("Posters");
        _publicBase = (posters["PublicBaseUrl"] ?? throw new InvalidOperationException("Posters:PublicBaseUrl ayarı eksik.")).TrimEnd('/');
        _variantWidths = posters.GetRequiredSection("VariantWidths").Get<int[]>()!;
        _defaultVariantWidth = posters.GetValue<int>("DefaultVariantWidth");
    }

    public string? ResolveUrl(string? posterKey)
        => string.IsNullOrWhiteSpace(posterKey) ? null : $"{_publicBase}/{posterKey}-{_defaultVariantWidth}.webp";

    public string? ResolveSrcset(string? posterKey)
        => string.IsNullOrWhiteSpace(posterKey)
            ? null
            : string.Join(", ", _variantWidths.Select(width => $"{_publicBase}/{posterKey}-{width}.webp {width}w"));
}
