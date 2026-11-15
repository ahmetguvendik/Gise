namespace Gise.Catalog.Application.Caching;

// Telgraf'ta yok. Yanıt her izleyiciye aynıysa true; yalnızca o zaman önbelleğe yazılır. İzleyiciye göre değişen yanıt
// (taslak etkinliğin detayı yalnızca sahibine ve admin'e görünür) false döner ve her seferinde handler'dan gelir; yoksa
// sahibinin gördüğü taslak, aynı anahtarla başkasına sızardı.
public interface ICacheableResponse
{
    bool CanBeCached { get; }
}
