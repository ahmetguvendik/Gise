#!/bin/sh
# APISIX'in başlangıç upstream ve route'ları: yönetim API'siyle etcd'ye yazılır. Sonrası dashboard'dan yönetilir
# (http://127.0.0.1:9180/ui/). Yeni kurulumda bir kez:
#   docker compose -f deploy/docker-compose.yml --profile setup up apisix-init
# Tekrar çalıştırılabilir; aynı id'li upstream ve route'lar buradaki hâline döner, dashboard'da onlarda yapılan
# değişiklikler gider. Kalıcı olması istenen değişiklik buraya da yazılmalı.
#
# Her API ucu ayrı bir route (yöntem + yol). Serviste yeni bir uç açılınca buraya da eklenir; eklenmezse gateway
# 404 ROUTE_NOT_FOUND döner.
set -e

ADMIN="http://apisix:9180/apisix/admin"

put() {
  status=$(curl -s -o /tmp/response -w "%{http_code}" -X PUT "$ADMIN/$1" \
    -H "X-API-KEY: $APISIX_ADMIN_KEY" -H "Content-Type: application/json" -d "$2")
  case "$status" in
    200|201) echo "  $1" ;;
    *) echo "  $1 yazılamadı (HTTP $status): $(cat /tmp/response)"; exit 1 ;;
  esac
}

# Yönetim API'si açılana kadar beklenir.
tries=0
until curl -sf -o /dev/null -H "X-API-KEY: $APISIX_ADMIN_KEY" "$ADMIN/routes"; do
  tries=$((tries + 1))
  if [ "$tries" -ge 30 ]; then
    echo "APISIX yönetim API'sine ulaşılamadı (anahtar yanlış ya da APISIX açılmadı)."
    exit 1
  fi
  sleep 2
done

upstream() {
  put "upstreams/$1" "{\"name\": \"$1\", \"type\": \"roundrobin\", \"nodes\": {\"$2\": 1}}"
}

# İstek sınırı: IP başına, dakikalık. Aynı gruptaki route'lar tek sayacı paylaşır, yani sınır bir kaynağın (ör.
# etkinlikler) bütün uçları için toplamdır; gruptaki her route'ta ayar birebir aynı olmalı (APISIX şartı). Grup boşsa
# route'un kendi sayacı olur. limit-count'un yanıtı düz metin; response-rewrite onu servislerin ProblemDetails
# biçimine çevirir, ön yüz mesajı gösterebilsin. Retry-After da burada eklenir.
limits() {
  group="$1"; count="$2"; title="$3"
  group_field=""
  if [ -n "$group" ]; then
    group_field="\"group\": \"$group\", "
  fi
  cat <<JSON
{
    "limit-count": {${group_field}"count": $count, "time_window": 60, "key_type": "var", "key": "remote_addr", "policy": "local", "rejected_code": 429},
    "response-rewrite": {
      "vars": [["status", "==", 429]],
      "headers": {"set": {"Content-Type": "application/problem+json; charset=utf-8", "Retry-After": "60"}},
      "body": "{\"type\":\"https://tools.ietf.org/html/rfc6585#section-4\",\"title\":\"$title\",\"status\":429,\"detail\":\"Lütfen bir dakika sonra tekrar deneyin.\",\"errorCode\":\"RATE_LIMIT_EXCEEDED\"}"
    }
  }
JSON
}

# Kaynak başına toplam sınır. Search'te yazarken öneri her tuşta değil kısa bir bekleme sonrası istenir; yine de
# diğerlerinden daha sık çağrılır.
group_limits() {
  case "$1" in
    search) limits search 600 "Çok fazla istek gönderdiniz." ;;
    *) limits "$1" 300 "Çok fazla istek gönderdiniz." ;;
  esac
}

# Bir uç: yöntem + yol. Yol parametreleri ":ad" biçiminde (config.yaml'daki router).
endpoint() {
  id="$1"; method="$2"; uri="$3"; upstream="$4"; group="$5"; desc="$6"
  put "routes/$id" "{
  \"name\": \"$method $uri\",
  \"desc\": \"$desc\",
  \"methods\": [\"$method\"],
  \"uri\": \"$uri\",
  \"upstream_id\": \"$upstream\",
  \"priority\": 10,
  \"plugins\": $(group_limits "$group")
}"
}

# Kötüye kullanıma açık uç: kendi sayacı ve daha dar sınırı var, kaynağın toplam sayacından düşmez.
strict_endpoint() {
  id="$1"; method="$2"; uri="$3"; upstream="$4"; count="$5"; title="$6"; desc="$7"
  put "routes/$id" "{
  \"name\": \"$method $uri\",
  \"desc\": \"$desc (dakikada $count)\",
  \"methods\": [\"$method\"],
  \"uri\": \"$uri\",
  \"upstream_id\": \"$upstream\",
  \"priority\": 20,
  \"plugins\": $(limits "" "$count" "$title")
}"
}

echo "Upstream'ler:"
upstream catalog catalog-api:8080
upstream reservation reservation-api:8080
upstream order order-api:8080
upstream ticket ticket-api:8080
upstream search search-api:8080
upstream client client:80

echo "Catalog: etkinlikler"
endpoint events-list     GET    /api/events                catalog events "Yayındaki etkinlikler"
endpoint events-mine     GET    /api/events/mine           catalog events "Organizatörün etkinlikleri, taslaklar dahil"
endpoint events-pending  GET    /api/events/pending        catalog events "Onay bekleyenler (admin)"
endpoint events-get      GET    /api/events/:id            catalog events "Etkinlik detayı"
endpoint events-create   POST   /api/events                catalog events "Taslak oluşturma"
endpoint events-update   PUT    /api/events/:id            catalog events "Ad, tarih ve mekân"
endpoint events-delete   DELETE /api/events/:id            catalog events "Taslağı silme"
endpoint events-prices   PUT    /api/events/:id/prices     catalog events "Bölüm fiyatları"
endpoint events-poster   PUT    /api/events/:id/poster     catalog events "Afiş"
endpoint events-submit   POST   /api/events/:id/submit     catalog events "Onaya gönderme"
endpoint events-withdraw POST   /api/events/:id/withdraw   catalog events "Onaydan geri çekme"
endpoint events-return   POST   /api/events/:id/return     catalog events "Taslağa geri gönderme (admin)"
endpoint events-publish  POST   /api/events/:id/publish    catalog events "Yayınlama (admin)"
endpoint events-cancel   POST   /api/events/:id/cancel     catalog events "İptal (admin)"

echo "Catalog: mekânlar"
endpoint venues-list   GET    /api/venues            catalog venues "Mekânlar"
endpoint venues-get    GET    /api/venues/:id        catalog venues "Mekân ve bölümleri"
endpoint venues-create POST   /api/venues            catalog venues "Mekân oluşturma (admin)"
endpoint venues-update PUT    /api/venues/:id        catalog venues "Ad ve şehir (admin)"
endpoint venues-layout PUT    /api/venues/:id/layout catalog venues "Bölüm düzeni (admin)"
endpoint venues-delete DELETE /api/venues/:id        catalog venues "Mekân silme (admin)"

echo "Catalog: afişler"
# Her izin depoya bir dosya yükletir; depo şişirmeye karşı dar.
strict_endpoint media-upload-url POST /api/media/upload-url catalog 20 "Çok fazla dosya yükleme isteği gönderdiniz." "Yükleme için imzalı adres"
endpoint media-commit POST /api/media/commit catalog media "Yüklenen afişi doğrulama ve boyutlandırma"
endpoint media-policy GET  /api/media/policy catalog media "Yükleme kuralları (tür, boyut)"

echo "Reservation"
endpoint reservation-seats GET /api/reservation/events/:eventId/seats reservation reservation "Koltuk durumu"

echo "Order"
# Her sipariş koltuk tutar; çok sayıda sipariş açıp koltukları satışa kapatmaya karşı dar.
strict_endpoint orders-create POST /api/orders order 10 "Çok fazla sipariş denemesi yaptınız." "Sipariş açma, koltukları tutar"
endpoint orders-mine   GET  /api/orders/mine       order orders "Müşterinin siparişleri"
endpoint orders-get    GET  /api/orders/:id        order orders "Sipariş durumu"
endpoint orders-pay    POST /api/orders/:id/pay    order orders "Ödeme"
endpoint orders-cancel POST /api/orders/:id/cancel order orders "Siparişten vazgeçme"

echo "Ticket"
endpoint tickets-mine     GET  /api/tickets/mine         ticket tickets "Müşterinin biletleri"
endpoint tickets-download GET  /api/tickets/:id/download ticket tickets "Bilet PDF'i (imzalı adres)"
endpoint tickets-check-in POST /api/tickets/check-in     ticket tickets "Kapıda okutma (görevli)"

echo "Search"
endpoint search-events  GET /api/search/events  search search "Ana sayfa listesi ve arama"
endpoint search-suggest GET /api/search/suggest search search "Yazarken öneri"

echo "Diğer"
# Tanımlı olmayan API yolu ya da yöntemi istemciye (index.html) düşmesin: 404 ProblemDetails.
put routes/api-not-found '{
  "name": "Tanımsız API yolu",
  "desc": "Yukarıdaki uçlara uymayan /api istekleri: 404",
  "uri": "/api/*",
  "priority": 1,
  "plugins": {
    "fault-injection": {"abort": {"http_status": 404, "body": "{\"type\":\"https://tools.ietf.org/html/rfc9110#section-15.5.5\",\"title\":\"İstenen adres bulunamadı.\",\"status\":404,\"errorCode\":\"ROUTE_NOT_FOUND\"}"}},
    "response-rewrite": {"vars": [["status", "==", 404]], "headers": {"set": {"Content-Type": "application/problem+json; charset=utf-8"}}}
  }
}'
put routes/client '{"name": "client", "desc": "React istemcisi: API yollarına uymayan her istek", "uri": "/*", "upstream_id": "client"}'

echo "APISIX hazır."
