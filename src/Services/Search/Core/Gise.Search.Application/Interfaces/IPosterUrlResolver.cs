namespace Gise.Search.Application.Interfaces;

// Afiş anahtarından herkese açık adresler; Catalog'un ürettiğiyle aynı biçim (posters/... -800.webp).
public interface IPosterUrlResolver
{
    string? ResolveUrl(string? posterKey);

    string? ResolveSrcset(string? posterKey);
}
