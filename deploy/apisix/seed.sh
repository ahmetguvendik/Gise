#!/bin/sh
# APISIX'in başlangıç upstream ve route'ları: yönetim API'siyle etcd'ye yazılır. Sonrası dashboard'dan yönetilir
# (http://127.0.0.1:9180/ui/). Yeni kurulumda bir kez:
#   docker compose -f deploy/docker-compose.yml --profile setup up apisix-init
# Tekrar çalıştırılabilir; aynı id'li upstream ve route'lar buradaki hâline döner, dashboard'da onlarda yapılan
# değişiklikler gider. Kalıcı olması istenen değişiklik buraya da yazılmalı.
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

# API route'ları istemci route'undan (/*) önce eşleşsin diye öncelikleri 10. İstek sınırı IP başına, dakikada $5.
api_route() {
  put "routes/$1" "{
    \"name\": \"$1\",
    \"desc\": \"$4\",
    \"uris\": $2,
    \"upstream_id\": \"$3\",
    \"priority\": 10,
    \"plugins\": {
      \"limit-count\": {
        \"count\": $5,
        \"time_window\": 60,
        \"key_type\": \"var\",
        \"key\": \"remote_addr\",
        \"policy\": \"local\",
        \"rejected_code\": 429
      }
    }
  }"
}

echo "Upstream'ler:"
upstream catalog catalog-api:8080
upstream reservation reservation-api:8080
upstream order order-api:8080
upstream ticket ticket-api:8080
upstream search search-api:8080
upstream client client:80

echo "Route'lar:"
api_route catalog '["/api/events", "/api/events/*", "/api/venues", "/api/venues/*", "/api/media/*"]' catalog \
  "Catalog: etkinlik, mekân ve afiş uçları" 300
api_route reservation '["/api/reservation/*"]' reservation "Reservation: koltuk durumu" 300
api_route order '["/api/orders", "/api/orders/*"]' order "Order: sipariş ve ödeme" 300
api_route ticket '["/api/tickets", "/api/tickets/*"]' ticket "Ticket: biletlerim, PDF, kapı" 300
# Yazarken öneri her tuşta değil, kısa bir bekleme sonrası istenir; yine de diğerlerinden daha sık çağrılır.
api_route search '["/api/search/*"]' search "Search: ana sayfa listesi, arama, öneri" 600
put routes/client '{"name": "client", "desc": "React istemcisi: API yollarına uymayan her istek", "uri": "/*", "upstream_id": "client"}'

echo "APISIX hazır."
