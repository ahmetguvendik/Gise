#!/usr/bin/env bash
# Demo için Keycloak'ta test kullanıcıları oluşturur (organizer@, customer@, admin@gise.local). Parola
# deploy/k8s/.secrets.env'deki DEV_USERS_PASSWORD; ekrana yazılmaz. Tekrar çalıştırılabilir.
set -euo pipefail

cd "$(dirname "$0")"
PW=$(grep -E '^DEV_USERS_PASSWORD=' .secrets.env | cut -d= -f2-)
[ -n "$PW" ] || { echo ".secrets.env içinde DEV_USERS_PASSWORD yok; önce deploy.sh çalıştırın." >&2; exit 1; }

kubectl -n gise exec -i keycloak-0 -c keycloak -- env DEV_USERS_PASSWORD="$PW" bash -s <<'INNER'
set -euo pipefail
kc=/opt/keycloak/bin/kcadm.sh
$kc config credentials --server http://localhost:8080/auth --realm master \
  --user "$KC_BOOTSTRAP_ADMIN_USERNAME" --password "$KC_BOOTSTRAP_ADMIN_PASSWORD" >/dev/null

create_user() {
  local email=$1 first=$2 role=${3:-}
  $kc create users -r gise -s username="$email" -s email="$email" -s emailVerified=true \
    -s enabled=true -s firstName="$first" -s lastName=Test >/dev/null 2>&1 || true
  $kc set-password -r gise --username "$email" --new-password "$DEV_USERS_PASSWORD"
  [ -z "$role" ] || $kc add-roles -r gise --uusername "$email" --rolename "$role"
  echo "hazır: $email ${role:+($role)}"
}

create_user organizer@gise.local Organizatör organizer
create_user customer@gise.local Müşteri
create_user admin@gise.local Yönetici admin
INNER
