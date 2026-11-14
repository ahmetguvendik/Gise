# Gişe — Kubernetes (k3s, ev ağı)

Tek makineli k3s kümesi: tüm servisler `gise` namespace'inde. Giriş noktası `http://<makine IP>:8000` (APISIX), afiş
yükleme/okuma `http://<makine IP>:9000` (MinIO). Yalnızca ev ağından erişilir.

Yapı: `base/` her şeyin tanımı, `overlays/homelab/` ev ağı adresi ve imaj adları. Yapılandırma dosyaları docker-compose ile
ortaktır (`deploy/apisix`, `deploy/keycloak`...), bu yüzden derleme `--load-restrictor LoadRestrictionsNone` ile yapılır
(betikler bunu kendisi yapar).

## 1. İmajlar

GitHub Actions (`.github/workflows/images.yml`) `main`'e her `src/**` değişikliğinde 24 imajı derleyip
`ghcr.io/ahmetguvendik/gise-<ad>:latest` olarak yayınlar (Actions sekmesinden elle de başlatılır).
Paketler ilk yayında **private** olur; iki yol var:
- Her paketi GitHub'da *Package settings → Change visibility → Public* yapmak (repo zaten public), ya da
- `read:packages` yetkili bir token ile `GHCR_USER` ve `GHCR_TOKEN` verip `deploy.sh`'ı çalıştırmak (pull secret'ı kurar).

## 2. Makine (bir kez, sunucuda)

```bash
sudo bash node-setup.sh 192.168.1.0/24     # ev ağınızın alt ağı
```

## 3. Bu bilgisayardan kubectl

```bash
mkdir -p ~/.kube
ssh <kullanıcı>@<IP> 'sudo cat /etc/rancher/k3s/k3s.yaml' | sed 's/127.0.0.1/<IP>/' > ~/.kube/gise-config
chmod 600 ~/.kube/gise-config
export KUBECONFIG=~/.kube/gise-config
kubectl get nodes
```

## 4. Kurulum / güncelleme

```bash
./deploy/k8s/deploy.sh <IP>      # ilk seferde IP; sonrasında parametresiz
./deploy/k8s/dev-users.sh        # demo kullanıcıları
```

Parolalar ilk çalıştırmada üretilir ve `deploy/k8s/.secrets.env`'de kalır (git'e girmez; kaybederseniz volume'lardaki
hesaplar eşleşmez, yedekleyin). SMTP ve Slack adresi `deploy/.env`'den okunur.

## Erişimler

| Ne | Nasıl |
|---|---|
| Uygulama | `http://<IP>:8000` |
| Keycloak yönetimi | `kubectl -n gise port-forward svc/keycloak 8180:8080` → `http://localhost:8180/auth/admin` |
| Kibana | `kubectl -n gise port-forward svc/kibana 5601` (kullanıcı `elastic`) |
| APISIX dashboard | `kubectl -n gise port-forward svc/apisix 9180` → `http://localhost:9180/ui/` |
| Sağlık paneli | `kubectl -n gise port-forward svc/healthcheck-api 9095:8080` |

Parolalar için: `grep ELASTIC_PASSWORD deploy/k8s/.secrets.env` gibi.

## Sorun giderme

```bash
kubectl -n gise get pods
kubectl -n gise logs deploy/order-api
kubectl -n gise describe pod <ad>
```
