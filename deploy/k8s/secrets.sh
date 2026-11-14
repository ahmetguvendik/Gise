#!/usr/bin/env bash
# gise-secrets Secret'ını üretir ve kümeye yazar. Parolalar ilk çalıştırmada rastgele üretilir ve deploy/k8s/.secrets.env'de
# (git'e girmez) saklanır: sonraki çalıştırmalar aynı değerleri kullanır, böylece veri volume'larındaki hesaplar bozulmaz.
# SMTP ve Slack adresi varsa deploy/.env'den okunur.
#
# Kullanım: ./deploy/k8s/secrets.sh
set -euo pipefail

cd "$(dirname "$0")"
FILE=.secrets.env
touch "$FILE"; chmod 600 "$FILE"

get() { grep -E "^$1=" "$FILE" | head -1 | cut -d= -f2- || true; }
rand() { LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c "${1:-32}"; }

# Yalnızca harf ve rakam: bağlantı dizelerinde ve kabuk komutlarında tırnak sorunu çıkarmaz.
for k in POSTGRES_PASSWORD KEYCLOAK_ADMIN_PASSWORD DEV_USERS_PASSWORD REDIS_PASSWORD RABBITMQ_PASSWORD MINIO_ROOT_PASSWORD \
         ELASTIC_PASSWORD APISIX_ADMIN_KEY KIBANA_PASSWORD LOGSTASH_PASSWORD APM_PASSWORD HEALTH_PASSWORD AUDIT_PASSWORD \
         IDENTITY_CLIENT_SECRET IDENTITY_ROLES_CLIENT_SECRET; do
  [ -n "$(get $k)" ] || echo "$k=$(rand 32)" >> "$FILE"
done
[ -n "$(get KIBANA_ENCRYPTION_KEY)" ] || echo "KIBANA_ENCRYPTION_KEY=$(rand 48)" >> "$FILE"

# İsteğe bağlı değerler: deploy/.env'de varsa oradan (boş kalabilir; Keycloak boş SMTP ile açılır).
fromenv() { [ -f ../.env ] && grep -E "^$1=" ../.env | head -1 | cut -d= -f2- || true; }
args=()
while IFS='=' read -r k v; do
  [ -n "$k" ] && args+=(--from-literal="$k=$v")
done < "$FILE"
for k in SMTP_USERNAME SMTP_PASSWORD HEALTH_WEBHOOK_URL; do
  args+=(--from-literal="$k=$(fromenv $k)")
done

kubectl -n gise create secret generic gise-secrets "${args[@]}" --dry-run=client -o yaml | kubectl apply -f -
echo "gise-secrets hazır ($(( ${#args[@]} )) anahtar)."
