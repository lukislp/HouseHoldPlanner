FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4 AS base
WORKDIR /app
EXPOSE 5242

FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["HaushaltsPlaner.Server/HaushaltsPlaner.Server.csproj", "HaushaltsPlaner.Server/packages.lock.json", "HaushaltsPlaner.Server/"]
COPY ["HaushaltsPlaner.Shared/HaushaltsPlaner.Shared.csproj", "HaushaltsPlaner.Shared/packages.lock.json", "HaushaltsPlaner.Shared/"]
# --locked-mode: the restore must match the committed packages.lock.json files exactly.
RUN dotnet restore "HaushaltsPlaner.Server/HaushaltsPlaner.Server.csproj" --locked-mode
COPY . .
RUN dotnet publish "HaushaltsPlaner.Server/HaushaltsPlaner.Server.csproj" -c Release -o /app/publish -p:RestoreLockedMode=true

FROM base AS final
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .
# Don't run as root: the aspnet base image has shipped its own "app" user (UID 1654) since
# .NET 8, so no user has to be created here - the same pattern the other .NET services in this
# account use. Two locations have to stay writable at runtime: /app/data holds the SQLite
# database (docker-compose.yml mounts the db-data volume there) and /app/wwwroot/uploads
# receives profile pictures and household background images, which ProfileService/HomeService
# create relative to the working directory.
# The chown has to happen BEFORE the VOLUME declaration so the ownership is baked into the
# image layer: a freshly created named volume inherits content and ownership from the image on
# its first mount. Watch out for an EXISTING deployment - a volume already populated with
# root-owned files keeps that ownership, and a one-time
#   docker run --rm -v haushaltsplaner_db-data:/d alpine chown -R 1654:1654 /d
# is needed there, otherwise the app user can no longer write to the database.
RUN mkdir -p /app/data /app/wwwroot/uploads/profiles /app/wwwroot/uploads/backgrounds \
    && chown -R app:app /app/data /app/wwwroot
VOLUME ["/app/data"]
USER app
ENTRYPOINT ["dotnet", "HaushaltsPlaner.Server.dll"]
