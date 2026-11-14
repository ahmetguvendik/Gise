#!/usr/bin/env bash
# Ubuntu Server 24.04 makinesinde (sunucunun kendisinde, sudo ile) bir kez çalışır: Elasticsearch ve Kubernetes için
# çekirdek ayarları, güvenlik duvarı ve k3s kurulumu.
#
# Kullanım (sunucuda):  sudo bash node-setup.sh <ev ağı alt ağı, örn. 192.168.1.0/24>
set -euo pipefail

[ "$(id -u)" = 0 ] || { echo "sudo ile çalıştırın." >&2; exit 1; }
LAN=${1:?Ev ağı alt ağını verin, örn. 192.168.1.0/24}

# Kubernetes swap ile çalışmaz.
swapoff -a
sed -i '/\sswap\s/ s/^/#/' /etc/fstab

# Elasticsearch için.
echo 'vm.max_map_count=262144' > /etc/sysctl.d/99-gise.conf
sysctl --system >/dev/null

# Güvenlik duvarı: SSH ve uygulama portları yalnızca ev ağından; k3s'in kendi trafiği (pod/servis ağları) serbest.
apt-get install -y ufw >/dev/null
ufw default deny incoming
ufw default allow outgoing
ufw allow from "$LAN" to any port 22 proto tcp
ufw allow from "$LAN" to any port 8000 proto tcp   # uygulama (APISIX)
ufw allow from "$LAN" to any port 9000 proto tcp   # MinIO (tarayıcı afişi buradan yükler/okur)
ufw allow from "$LAN" to any port 6443 proto tcp   # kubectl (kubeconfig ile bu bilgisayardan)
ufw allow from 10.42.0.0/16
ufw allow from 10.43.0.0/16
ufw --force enable

# k3s: Traefik kapalı (giriş noktası APISIX), kubeconfig herkesçe okunabilir olmasın.
curl -sfL https://get.k3s.io | INSTALL_K3S_EXEC="server --disable traefik --write-kubeconfig-mode 600" sh -

kubectl get nodes
echo
echo "Kubeconfig: /etc/rancher/k3s/k3s.yaml (README'deki adımla kendi bilgisayarınıza alın)."
