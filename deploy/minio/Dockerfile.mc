# mc (MinIO istemcisi) kurulum işi için; aynı nedenle kaynaktan derlenir.
FROM golang:1.24-alpine AS build
ARG MC_VERSION=RELEASE.2025-04-16T18-13-26Z
RUN CGO_ENABLED=0 GOBIN=/out go install -trimpath github.com/minio/mc@${MC_VERSION}

FROM alpine:3.21
RUN apk add --no-cache ca-certificates curl
COPY --from=build /out/mc /usr/bin/mc
ENTRYPOINT ["/usr/bin/mc"]
