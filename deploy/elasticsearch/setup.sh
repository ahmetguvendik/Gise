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

# Logstash: yalnızca üç log akışına (API, worker, denetim) yazar.
echo "logstash_writer rolü ve kullanıcısı"
call PUT "$ES/_security/role/logstash_writer" '{
  "cluster": ["monitor"],
  "indices": [{
    "names": ["logs-gise.api-*", "logs-gise.workers-*", "logs-gise.audit-*"],
    "privileges": ["auto_configure", "create_doc", "view_index_metadata"]
  }]
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

# Sağlık paneli (healthcheck-api): küme durumunu okur ve son dakikalardaki hata loglarını sayar; hiçbir şey yazmaz. Denetim
# akışına (kişisel veri) erişemez: yalnızca API ve worker logları.
echo "health_reader rolü ve kullanıcısı"
call PUT "$ES/_security/role/health_reader" '{
  "cluster": ["monitor"],
  "indices": [{ "names": ["logs-gise.api-*", "logs-gise.workers-*"], "privileges": ["read", "view_index_metadata"] }]
}'
call PUT "$ES/_security/user/health_reader" "{\"password\":\"${HEALTH_PASSWORD}\",\"roles\":[\"health_reader\"]}"

# Denetim sayfası (audit-api): yalnızca denetim akışını (kişisel veri: kimlik, IP) okur; hiçbir şey yazmaz. health_reader'ın
# göremediği tek akış bu; iki hesap bilerek ayrı.
echo "audit_reader rolü ve kullanıcısı"
call PUT "$ES/_security/role/audit_reader" '{
  "indices": [{ "names": ["logs-gise.audit-*"], "privileges": ["read", "view_index_metadata"] }]
}'
call PUT "$ES/_security/user/audit_reader" "{\"password\":\"${AUDIT_PASSWORD}\",\"roles\":[\"audit_reader\"]}"

# Log akışı şablonu: $1 akış adı (sonuna -default eklenir), $2 saklama, $3 açıklama. Yerleşik logs-*-* şablonundan önce
# gelir (öncelik 200); ECS alan eşlemeleri yerleşik bileşenlerden. Tek düğüm: kopya yok. Var olan akış şablon değişikliğini
# yalnızca sonraki indekslerinde gördüğü için saklama süresi akışa da yazılır.
log_stream() {
  echo "$1 şablonu ($2)"
  call PUT "$ES/_index_template/$1" "{
    \"index_patterns\": [\"$1-*\"],
    \"data_stream\": {},
    \"priority\": 200,
    \"composed_of\": [\"logs@mappings\", \"ecs@mappings\"],
    \"template\": {
      \"settings\": { \"number_of_replicas\": 0 },
      \"lifecycle\": { \"data_retention\": \"$2\" }
    },
    \"_meta\": { \"description\": \"$3\" }
  }"
  if curl -s -u "$AUTH" "$ES/_data_stream/$1-default" | grep -q '"name"'; then
    call PUT "$ES/_data_stream/$1-default/_lifecycle" "{ \"data_retention\": \"$2\" }"
  fi
}

# Üç akış (bkz. tasarım 8 ve deploy/logstash/pipeline/logs.conf).
log_stream logs-gise.api 7d "Gişe API logları: her istek için bir satır, uyarı ve hatalar; 7 gün sonra silinir."
log_stream logs-gise.workers 7d "Gişe worker logları ve sistem adımları; 7 gün sonra silinir."
log_stream logs-gise.audit 365d "Gişe denetim kayıtları: insanların yaptığı işlemler; 1 yıl sonra silinir."

echo "Kibana bekleniyor (ilk açılışta 1-2 dakika sürebilir)..."
wait_for "$KIBANA/api/status" '"level":"available"'
# "Hazır" dedikten sonra da lisansı Elasticsearch'ten alana kadar yazma uçları 503 dönebiliyor (kibana_system parolası
# az önce atandıysa); veri görünümü listesi gelene kadar beklenir.
wait_for "$KIBANA/api/data_views" '"data_view"'

# Kibana veri görünümü: $1 kimlik, $2 ad, $3 akış kalıbı.
data_view() {
  echo "Kibana görünümü: $2"
  call POST "$KIBANA/api/data_views/data_view" "{
    \"override\": true,
    \"data_view\": { \"id\": \"$1\", \"name\": \"$2\", \"title\": \"$3\", \"timeFieldName\": \"@timestamp\" }
  }"
}

data_view gise-api "Gişe API logları" "logs-gise.api-*"
data_view gise-workers "Gişe worker logları" "logs-gise.workers-*"
data_view gise-audit "Gişe denetim kayıtları" "logs-gise.audit-*"
call POST "$KIBANA/api/data_views/default" '{ "data_view_id": "gise-api", "force": true }'

# Tek akış dönemindeki görünüm (logs-gise-*); yoksa 404 döner, sorun değil.
curl -s -o /dev/null -u "$AUTH" -X DELETE "$KIBANA/api/data_views/data_view/gise-logs" -H "kbn-xsrf: true"

# APM'in indeks şablonları Elasticsearch'ün kendisinde (apm-data eklentisi); yoksa APM arayüzü servis listesini boş
# döndürür (Telgraf dersi).
if curl -s -u "$AUTH" "$ES/_index_template/traces-apm@template" | grep -q '"name"'; then
  echo "APM şablonları var"
else
  echo "UYARI: traces-apm@template bulunamadı; APM arayüzü boş kalabilir."
fi

echo "Hazır. Kibana: http://127.0.0.1:5601 (kullanıcı elastic, parola deploy/.env'deki ELASTIC_PASSWORD)"
