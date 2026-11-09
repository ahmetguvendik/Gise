# Build context: repo kökü (deploy/docker-compose.yml böyle derler).

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Compose healthcheck'i sağlık ucuna istek atıyor; aspnet imajında HTTP istemcisi yok.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
USER $APP_UID
# Yalnızca sağlık ucu.
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/ src/Shared/
COPY src/Services/Identity/ src/Services/Identity/
COPY src/Workers/Identity/Gise.Identity.AuditWorker/ src/Workers/Identity/Gise.Identity.AuditWorker/
# NuGet paketleri build'ler arasında önbellekte kalır; kod değişince restore saniyeler sürer.
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/Workers/Identity/Gise.Identity.AuditWorker/Gise.Identity.AuditWorker.csproj -c Release -o /app/publish

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gise.Identity.AuditWorker.dll"]
