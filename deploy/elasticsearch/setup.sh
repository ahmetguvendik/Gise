#!/bin/sh
# Elasticsearch hesapları, log ve denetim kaydı saklama süreleri, Kibana'daki görünümler (bkz. tasarım 8). Tekrar çalıştırılabilir: her
# adım "varsa üzerine yaz" biçiminde. Yeni kurulumda bir kez:
#   docker compose -f deploy/docker-compose.yml --profile setup up elastic-init
# Parolalar deploy/.env'den ortam değişkeniyle gelir; hiçbiri yazdırılmaz.
set -eu

ES="http://elasticsearch:9200"
KIBANA="http://kibana:5601"
AUTH="elastic:${ELASTIC_PASSWORD}"

call() {
  # $1 yöntem, $2 adres, $3 gövde; kbn-xsrf Kibana'nın yazma uçlarında zorunlu, Elasticsearch yok sayar.
  status=$(curl -s -o /tmp/response -w "%{http_code}" -u "$AUTH" -X "$1" "$2" \
    -H "Content-Type: application/json" -H "kbn-xsrf: true" ${3:+-d "$3"})
  case "$status" in
    2*) ;;
    *) echo "  $1 $2 başarısız (HTTP $status): $(cat /tmp/response)"; exit 1 ;;
  esac
}

wait_for() {
  tries=0
  until curl -s -u "$AUTH" "$1" | grep -q "$2"; do
    tries=$((tries + 1))
    if [ "$tries" -ge 90 ]; then
      echo "  $1 hazır olmadı."
      exit 1
    fi
    sleep 2
  done
}

echo "Elasticsearch bekleniyor..."
wait_for "$ES/_cluster/health" '"status"'

# Kibana bu yerleşik kullanıcıyla bağlanır; elastic süper kullanıcısını kullanmaz. Kullanıcı oluşturulmaz, parolası atanır.
echo "kibana_system parolası"
call POST "$ES/_security/user/kibana_system/_password" "{\"password\":\"${KIBANA_PASSWORD}\"}"

# Logstash: yalnızca uygulama loglarının ve denetim kayıtlarının veri akışlarına yazar.
echo "logstash_writer rolü ve kullanıcısı"
call PUT "$ES/_security/role/logstash_writer" '{
  "cluster": ["monitor"],
  "indices": [{ "names": ["logs-gise-*", "logs-gise.audit-*"], "privileges": ["auto_configure", "create_doc", "view_index_metadata"] }]
}'
call PUT "$ES/_security/user/logstash_writer" "{\"password\":\"${LOGSTASH_PASSWORD}\",\"roles\":[\"logstash_writer\"]}"

# APM Server: yalnızca APM veri akışlarına yazar (Telgraf'ta elastic süper kullanıcısıydı).
echo "apm_writer rolü ve kullanıcısı"
# Ajanların merkezî ayarı (.apm-agent-configuration) sistem indeksinde; okumak için ayrıca izin gerekir.
call PUT "$ES/_security/role/apm_writer" '{
  "cluster": ["monitor"],
  "indices": [
    { "names": ["traces-apm*", "metrics-apm*", "logs-apm*"], "privileges": ["auto_configure", "create_doc"] },
    { "names": [".apm-agent-configuration"], "privileges": ["read"], "allow_restricted_indices": true }
  ]
}'
call PUT "$ES/_security/user/apm_writer" "{\"password\":\"${APM_PASSWORD}\",\"roles\":[\"apm_writer\"]}"

# Uygulama logları: tek veri akışı (logs-gise-default), 7 gün sonra silinir. Yerleşik logs-*-* şablonundan önce gelir
# (öncelik 200); ECS alan eşlemeleri yerleşik bileşenlerden. Tek düğüm: kopya yok.
echo "logs-gise şablonu (7 gün)"
call PUT "$ES/_index_template/logs-gise" '{
  "index_patterns": ["logs-gise-*"],
  "data_stream": {},
  "priority": 200,
  "composed_of": ["logs@mappings", "ecs@mappings"],
  "template": {
    "settings": { "number_of_replicas": 0 },
    "lifecycle": { "data_retention": "7d" }
  },
  "_meta": { "description": "Gişe uygulama logları; 7 gün sonra silinir." }
}'

# Denetim kayıtları (kim ne yaptı; bkz. CommandLoggingBehavior): ayrı akış (logs-gise.audit-default), 1 yıl. "logs-gise-*"
# kalıbına girmez: veri kümesi adında nokta var.
echo "logs-gise.audit şablonu (1 yıl)"
call PUT "$ES/_index_template/logs-gise.audit" '{
  "index_patterns": ["logs-gise.audit-*"],
  "data_stream": {},
  "priority": 200,
  "composed_of": ["logs@mappings", "ecs@mappings"],
  "template": {
    "settings": { "number_of_replicas": 0 },
    "lifecycle": { "data_retention": "365d" }
  },
  "_meta": { "description": "Gişe denetim kayıtları (kim ne yaptı); 1 yıl sonra silinir." }
}'

# Var olan akış şablon değişikliğini yalnızca sonraki indekslerinde görür; saklama süresi akışa da yazılır.
if curl -s -u "$AUTH" "$ES/_data_stream/logs-gise-default" | grep -q '"name"'; then
  call PUT "$ES/_data_stream/logs-gise-default/_lifecycle" '{ "data_retention": "7d" }'
fi
if curl -s -u "$AUTH" "$ES/_data_stream/logs-gise.audit-default" | grep -q '"name"'; then
  call PUT "$ES/_data_stream/logs-gise.audit-default/_lifecycle" '{ "data_retention": "365d" }'
fi

echo "Kibana bekleniyor (ilk açılışta 1-2 dakika sürebilir)..."
wait_for "$KIBANA/api/status" '"level":"available"'
# "Hazır" dedikten sonra da lisansı Elasticsearch'ten alana kadar yazma uçları 503 dönebiliyor (kibana_system parolası
# az önce atandıysa); veri görünümü listesi gelene kadar beklenir.
wait_for "$KIBANA/api/data_views" '"data_view"'

echo "Kibana log görünümü"
call POST "$KIBANA/api/data_views/data_view" '{
  "override": true,
  "data_view": { "id": "gise-logs", "name": "Gişe logları", "title": "logs-gise-*", "timeFieldName": "@timestamp" }
}'
call POST "$KIBANA/api/data_views/default" '{ "data_view_id": "gise-logs", "force": true }'

echo "Kibana denetim görünümü"
call POST "$KIBANA/api/data_views/data_view" '{
  "override": true,
  "data_view": { "id": "gise-audit", "name": "Gişe denetim kayıtları", "title": "logs-gise.audit-*", "timeFieldName": "@timestamp" }
}'

# APM'in indeks şablonları Elasticsearch'ün kendisinde (apm-data eklentisi); yoksa APM arayüzü servis listesini boş
# döndürür (Telgraf dersi).
if curl -s -u "$AUTH" "$ES/_index_template/traces-apm@template" | grep -q '"name"'; then
  echo "APM şablonları var"
else
  echo "UYARI: traces-apm@template bulunamadı; APM arayüzü boş kalabilir."
fi

echo "Hazır. Kibana: http://127.0.0.1:5601 (kullanıcı elastic, parola deploy/.env'deki ELASTIC_PASSWORD)"
