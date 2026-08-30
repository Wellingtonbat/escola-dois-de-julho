# syntax=docker/dockerfile:1

# --- Etapa 1: build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia primeiro só os arquivos de projeto (e o Directory.Build.props, que define a versão)
# para aproveitar o cache de camadas do Docker no "dotnet restore".
COPY Directory.Build.props ./
COPY src/SistemaEscolar.Domain/SistemaEscolar.Domain.csproj src/SistemaEscolar.Domain/
COPY src/SistemaEscolar.Shared/SistemaEscolar.Shared.csproj src/SistemaEscolar.Shared/
COPY src/SistemaEscolar.Application/SistemaEscolar.Application.csproj src/SistemaEscolar.Application/
COPY src/SistemaEscolar.Infrastructure/SistemaEscolar.Infrastructure.csproj src/SistemaEscolar.Infrastructure/
COPY src/SistemaEscolar.Web/SistemaEscolar.Web.csproj src/SistemaEscolar.Web/

RUN dotnet restore src/SistemaEscolar.Web/SistemaEscolar.Web.csproj

# Agora copia o restante do código-fonte (sem tests/, ignorado via .dockerignore) e publica.
COPY src/ src/

RUN dotnet publish src/SistemaEscolar.Web/SistemaEscolar.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# --- Etapa 2: runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

# O Render (e serviços similares) injeta a porta a ser usada na variável de ambiente PORT.
# Fazemos o bind dinâmico dela para ASPNETCORE_URLS no start do container.
EXPOSE 8080
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet SistemaEscolar.Web.dll"]
