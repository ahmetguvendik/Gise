#!/bin/sh
# MinIO bucket kurulumu. Tekrar tekrar çalıştırılabilir: her adım "zaten var" durumunu hatasız geçer.
#   docker compose -f deploy/docker-compose.yml --profile setup up minio-init
set -e

mc alias set local http://minio:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"

mc mb --ignore-existing "local/$MINIO_BUCKET"

# Yalnızca afişler herkese açık okunur: her görüntüleme için imzalı adres üretmek tarayıcı önbelleğini işlevsiz kılardı.
# staging/ (doğrulanmamış yüklemeler) ve ileride biletler gibi özel dosyalar açık değildir.
# Yazma izni yok; yükleme yalnızca catalog-api'nin ürettiği imzalı adresle yapılır.
mc anonymous set download "local/$MINIO_BUCKET/posters"

# Etkinliğe bağlanmadan yarım kalan yüklemeler çöp olarak birikmesin.
cat > /tmp/lifecycle.json <<JSON
{
  "Rules": [
    {
      "ID": "expire-staging",
      "Status": "Enabled",
      "Filter": { "Prefix": "staging/" },
      "Expiration": { "Days": 1 }
    }
  ]
}
JSON
mc ilm import "local/$MINIO_BUCKET" < /tmp/lifecycle.json

echo "MinIO hazır: bucket=$MINIO_BUCKET (posters/ herkese açık okunur, staging/ 1 gün sonra silinir)"
mc anonymous list "local/$MINIO_BUCKET" || true
mc ilm rule ls "local/$MINIO_BUCKET" || true
