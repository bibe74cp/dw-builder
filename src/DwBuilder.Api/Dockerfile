# ──────────────────────────────────────────────────────────────────────────────
# Stage 1: Build
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to leverage Docker layer caching on restore
COPY src/DwBuilder.Core/DwBuilder.Core.csproj             src/DwBuilder.Core/
COPY src/DwBuilder.Infrastructure/DwBuilder.Infrastructure.csproj src/DwBuilder.Infrastructure/
COPY src/DwBuilder.Biml/DwBuilder.Biml.csproj              src/DwBuilder.Biml/
COPY src/DwBuilder.Api/DwBuilder.Api.csproj                src/DwBuilder.Api/

RUN dotnet restore src/DwBuilder.Api/DwBuilder.Api.csproj

# Copy full source and publish
COPY src/ src/
RUN dotnet publish src/DwBuilder.Api/DwBuilder.Api.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: Runtime
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "DwBuilder.Api.dll"]
