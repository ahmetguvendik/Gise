#!/usr/bin/env bash
# Gişe'yi k3s kümesine kurar ya da günceller. Tekrar çalıştırılabilir; veri volume'larına dokunmaz.
#
# Kullanım:  ./deploy/k8s/deploy.sh <makinenin ev ağı IP'si>     (ilk kurulumda IP verilir, sonra yazılı kalır)
# Ortam:     KUBECONFIG kümeyi göstermeli (README). İmajlar özel pakette ise GHCR_USER ve GHCR_TOKEN (read:packages) verilir.
set -euo pipefail

cd "$(dirname "$0")"
OVERLAY=overlays/homelab

if [ $# -ge 1 ]; then
  printf 'PUBLIC_URL=http://%s:8000\nMINIO_PUBLIC_URL=http://%s:9000\n' "$1" "$1" > $OVERLAY/public.env
elif ! grep -q 'PUBLIC_URL' $OVERLAY/public.env 2>/dev/null || grep -q '192.168.1.50' $OVERLAY/public.env 2>/dev/null; then
  echo "İlk kurulumda makinenin IP'sini verin: ./deploy/k8s/deploy.sh 192.168.x.y" >&2; exit 1
fi
cat $OVERLAY/public.env

kubectl apply -f base/namespace.yaml
./secrets.sh

if [ -n "${GHCR_TOKEN:-}" ]; then
  kubectl -n gise create secret docker-registry ghcr-pull --docker-server=ghcr.io \
    --docker-username="${GHCR_USER:?GHCR_USER gerekli}" --docker-password="$GHCR_TOKEN" --dry-run=client -o yaml | kubectl apply -f -
fi

# İşler değişmez nesnelerdir: eskiler silinip yenisi çalıştırılır (tekrar çalıştırılabilir betikler).
kubectl -n gise delete job minio-init elastic-init apisix-init --ignore-not-found --wait=true

kubectl kustomize --load-restrictor LoadRestrictionsNone $OVERLAY | kubectl apply -f -

echo "Altyapı bekleniyor..."
for s in postgres redis kafka rabbitmq minio elasticsearch etcd keycloak; do
  kubectl -n gise rollout status statefulset/$s --timeout=600s
done
for d in apisix kibana; do
  kubectl -n gise rollout status deployment/$d --timeout=600s
done
echo "Kurulum işleri bekleniyor..."
kubectl -n gise wait --for=condition=complete job/minio-init job/elastic-init job/apisix-init --timeout=600s

echo "Uygulamalar bekleniyor (ilk açılışta veritabanı şemaları kurulur, birkaç dakika sürebilir)..."
kubectl -n gise rollout status deployment --timeout=900s || true
kubectl -n gise get pods
echo
echo "Hazır: $(grep ^PUBLIC_URL $OVERLAY/public.env | cut -d= -f2-)"
echo "Test kullanıcıları için: ./deploy/k8s/dev-users.sh"
