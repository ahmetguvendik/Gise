FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --no-fund --no-audit
COPY . .
RUN npm run build

FROM nginx:1.27-alpine
# Şablonlar: nginx imajı açılışta tanımlı ortam değişkenlerini yerine koyar ve conf.d'ye yazar. Güvenlik
# başlıklarındaki (CSP) adresler bunlar; varsayılanlar yerel geliştirme, canlıda compose'dan verilir.
ENV KEYCLOAK_ORIGIN=http://localhost:8180 \
    MINIO_ORIGIN=http://localhost:9000
COPY nginx.conf /etc/nginx/templates/default.conf.template
COPY security-headers.conf /etc/nginx/templates/security-headers.inc.template
COPY --from=build /app/dist /usr/share/nginx/html
