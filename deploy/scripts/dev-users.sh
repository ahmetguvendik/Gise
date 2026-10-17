#!/usr/bin/env bash
# Yerel geliştirme için Keycloak'ta test kullanıcıları oluşturur:
#   organizer@gise.local  (organizer rolü)
#   customer@gise.local   (yalnızca varsayılan customer rolü)
#   admin@gise.local      (admin rolü)
# Parola deploy/.env'deki DEV_USERS_PASSWORD; ekrana yazılmaz. Tekrar çalıştırılabilir.
#
# Kullanım (repo kökünden):  ./deploy/scripts/dev-users.sh
set -euo pipefail

cd "$(dirname "$0")/.."
DEV_USERS_PASSWORD=$(grep -E '^DEV_USERS_PASSWORD=' .env | cut -d= -f2-)
[ -n "$DEV_USERS_PASSWORD" ] || { echo "deploy/.env içinde DEV_USERS_PASSWORD boş." >&2; exit 1; }

docker compose exec -T -e DEV_USERS_PASSWORD="$DEV_USERS_PASSWORD" keycloak bash -s <<'EOF'
set -euo pipefail
kc=/opt/keycloak/bin/kcadm.sh
$kc config credentials --server http://localhost:8080 --realm master \
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
EOF
