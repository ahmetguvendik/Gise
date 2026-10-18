#!/usr/bin/env bash
# Keycloak'ın e-posta ayarını (Gmail SMTP) deploy/.env'deki SMTP_USERNAME ve SMTP_PASSWORD ile günceller.
# Realm JSON'u yalnızca ilk kurulumda içe aktarıldığı için var olan kurulumda bu betik çalıştırılır.
# SMTP_PASSWORD bir Google uygulama şifresidir; ekrana yazılmaz. Tekrar çalıştırılabilir.
#
# Kullanım (repo kökünden):  ./deploy/scripts/keycloak-smtp.sh
set -euo pipefail

cd "$(dirname "$0")/.."
read_env() { grep -E "^$1=" .env | cut -d= -f2- | sed -e 's/^"//' -e 's/"$//'; }
SMTP_USERNAME=$(read_env SMTP_USERNAME)
SMTP_PASSWORD=$(read_env SMTP_PASSWORD)
[ -n "$SMTP_USERNAME" ] && [ -n "$SMTP_PASSWORD" ] || { echo "deploy/.env içinde SMTP_USERNAME ve SMTP_PASSWORD dolu olmalı." >&2; exit 1; }

docker compose exec -T -e SMTP_USERNAME="$SMTP_USERNAME" -e SMTP_PASSWORD="$SMTP_PASSWORD" keycloak bash -s <<'EOF'
set -euo pipefail
kc=/opt/keycloak/bin/kcadm.sh
$kc config credentials --server http://localhost:8080 --realm master \
  --user "$KC_BOOTSTRAP_ADMIN_USERNAME" --password "$KC_BOOTSTRAP_ADMIN_PASSWORD" >/dev/null
# Gmail, gönderen olarak yalnızca giriş yapılan hesabı kabul eder.
$kc update realms/gise \
  -s smtpServer.host=smtp.gmail.com -s smtpServer.port=587 \
  -s smtpServer.starttls=true -s smtpServer.ssl=false -s smtpServer.auth=true \
  -s "smtpServer.user=$SMTP_USERNAME" -s "smtpServer.password=$SMTP_PASSWORD" \
  -s "smtpServer.from=$SMTP_USERNAME" -s 'smtpServer.fromDisplayName=Gişe'
echo "Keycloak e-posta ayarı güncellendi: smtp.gmail.com:587 ($SMTP_USERNAME)"
EOF
