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

FROM nginx:alpine@sha256:72ba65eb42c10344912a84ff42408db7d34f2feb642204570ab8fc5ffd29f1d3 AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY HaushaltsPlaner.Client/nginx.conf /etc/nginx/conf.d/default.conf
RUN echo "{}" > /usr/share/nginx/html/appsettings.json
EXPOSE 80
