#!/usr/bin/env bash
# Ev ağı için yerel CA ve sunucu sertifikası üretir, gise-tls Secret'ını kümeye yazar.
# CA ve anahtarlar deploy/k8s/.tls/ içinde kalır (git'e girmez). CA bir kez kurulur; sertifika IP değişirse yenilenir.
#
# Kullanım: ./deploy/k8s/tls.sh <makinenin ev ağı IP'si>
set -euo pipefail

IP=${1:?Makinenin IP adresini verin}
cd "$(dirname "$0")"
D=.tls; mkdir -p $D; chmod 700 $D

if [ ! -f $D/ca.key ]; then
  openssl genrsa -out $D/ca.key 4096 2>/dev/null
  openssl req -x509 -new -key $D/ca.key -sha256 -days 3650 -subj "/CN=Gise Yerel CA" -out $D/ca.crt
  echo "Yeni CA üretildi: deploy/k8s/.tls/ca.crt (cihazlara kurulacak)."
fi

# Sertifika yoksa ya da başka bir IP için üretilmişse yenile. Tarayıcılar IP için SAN şart koşar; ömür 825 günü geçmesin.
if [ ! -f $D/server.crt ] || [ "$(cat $D/server.ip 2>/dev/null)" != "$IP" ]; then
  openssl genrsa -out $D/server.key 2048 2>/dev/null
  openssl req -new -key $D/server.key -subj "/CN=$IP" -out $D/server.csr
  printf "subjectAltName=IP:%s\nbasicConstraints=CA:FALSE\nkeyUsage=digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth\n" "$IP" > $D/ext.cnf
  openssl x509 -req -in $D/server.csr -CA $D/ca.crt -CAkey $D/ca.key -CAcreateserial -days 800 -sha256 -extfile $D/ext.cnf -out $D/server.crt 2>/dev/null
  echo "$IP" > $D/server.ip
  echo "Sunucu sertifikası üretildi ($IP)."
fi

kubectl -n gise create secret tls gise-tls --cert=$D/server.crt --key=$D/server.key --dry-run=client -o yaml | kubectl apply -f -
