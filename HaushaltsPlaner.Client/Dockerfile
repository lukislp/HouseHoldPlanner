FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:2fa828c68761b1b8c23d7662dc134421b9d3b59fe1425fdbc80804e390cdb24d AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["HaushaltsPlaner.Client/HaushaltsPlaner.Client.csproj", "HaushaltsPlaner.Client/"]
COPY ["HaushaltsPlaner.Shared/HaushaltsPlaner.Shared.csproj", "HaushaltsPlaner.Shared/packages.lock.json", "HaushaltsPlaner.Shared/"]
# --locked-mode applies to the locked Shared project; the WebAssembly client itself has no
# lock file (see Directory.Build.props).
RUN dotnet restore "HaushaltsPlaner.Client/HaushaltsPlaner.Client.csproj" --locked-mode
COPY . .
RUN dotnet publish "HaushaltsPlaner.Client/HaushaltsPlaner.Client.csproj" -c Release -o /app/publish -p:WasmBuildNative=false -p:RunAOTCompilation=false -p:RestoreLockedMode=true

# nginx-unprivileged instead of the stock nginx image: there the master process runs as root
# so it can bind the privileged port 80. This variant runs entirely as UID 101 and listens on
# 8080 instead, which is why nginx.conf, docker-compose.yml and the README all name 8080 now.
# Its own /etc/nginx/nginx.conf already keeps the pid file and every temp directory under /tmp,
# so nothing outside the server block needs adjusting. The digest covers a multi-arch index
# that includes linux/arm64, which the release workflow builds a native leg for.
FROM nginxinc/nginx-unprivileged:alpine@sha256:19c132c9ab02d3b783f478743dafc7a7f42e27aa7d2bdcbec1bb1128ca8f2a07 AS final
# The base image already ends on USER 101; switch back to root for the copy steps only, because
# /usr/share/nginx/html is root-owned and the appsettings.json below is written into it.
USER root
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
# The site config keeps the base image's own nginx:nginx ownership: the entrypoint script
# 10-listen-on-ipv6-by-default.sh appends the IPv6 listener to it at container start and logs
# "can not modify ... (read-only file system?)" when it is root-owned instead.
COPY --chown=nginx:nginx HaushaltsPlaner.Client/nginx.conf /etc/nginx/conf.d/default.conf
RUN echo "{}" > /usr/share/nginx/html/appsettings.json \
    && chown -R nginx:nginx /usr/share/nginx/html
# Numeric, so an orchestrator can enforce runAsNonRoot without resolving the name first.
USER 101
EXPOSE 8080
