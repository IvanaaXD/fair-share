# FairShare API - container image.
# Build context: the Backend folder (the one that contains the four FairShare.* projects).
#   docker build -t fairshare-api .
# Normally started through docker-compose.yml in the repository root.

# ---------- build stage: full SDK, used only to compile ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first: "dotnet restore" is then cached as its own layer and only re-runs
# when a dependency changes, not on every code change.
COPY FairShare.Domain/FairShare.Domain.csproj                 FairShare.Domain/
COPY FairShare.Application/FairShare.Application.csproj       FairShare.Application/
COPY FairShare.Infrastructure/FairShare.Infrastructure.csproj FairShare.Infrastructure/
COPY FairShare.API/FairShare.API.csproj                       FairShare.API/
RUN dotnet restore FairShare.API/FairShare.API.csproj

# The rest of the source code.
COPY . .
RUN dotnet publish FairShare.API/FairShare.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------- runtime stage: only the ASP.NET Core runtime, much smaller ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Uploaded files (profile images, receipt photos) are written here; docker-compose mounts a
# volume on this path so they survive container restarts. The app also writes files next to
# itself (e.g. the initial admin password), so the non-root user must own /app.
RUN mkdir -p /app/uploads && chown -R $APP_UID:$APP_UID /app

# The official .NET images ship a non-root user; APP_UID is its id.
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FairShare.API.dll"]
