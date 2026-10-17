# Build context: repo kökü (deploy/docker-compose.yml böyle derler).

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Compose healthcheck'i /health/ready'e istek atıyor; aspnet imajında HTTP istemcisi yok.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
USER $APP_UID
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce yalnızca proje dosyaları: kod değişince restore katmanı önbellekten gelir.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Services/Catalog/Core/Gise.Catalog.Domain/Gise.Catalog.Domain.csproj src/Services/Catalog/Core/Gise.Catalog.Domain/
COPY src/Services/Catalog/Core/Gise.Catalog.Application/Gise.Catalog.Application.csproj src/Services/Catalog/Core/Gise.Catalog.Application/
COPY src/Services/Catalog/Infrastructure/Gise.Catalog.Persistence/Gise.Catalog.Persistence.csproj src/Services/Catalog/Infrastructure/Gise.Catalog.Persistence/
COPY src/Services/Catalog/Presentation/Gise.Catalog.Api/Gise.Catalog.Api.csproj src/Services/Catalog/Presentation/Gise.Catalog.Api/
RUN dotnet restore src/Services/Catalog/Presentation/Gise.Catalog.Api/Gise.Catalog.Api.csproj

COPY src/Services/Catalog/ src/Services/Catalog/
RUN dotnet publish src/Services/Catalog/Presentation/Gise.Catalog.Api/Gise.Catalog.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gise.Catalog.Api.dll"]
