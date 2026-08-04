FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["HaushaltsPlaner.Client/HaushaltsPlaner.Client.csproj", "HaushaltsPlaner.Client/"]
COPY ["HaushaltsPlaner.Shared/HaushaltsPlaner.Shared.csproj", "HaushaltsPlaner.Shared/"]
RUN dotnet restore "HaushaltsPlaner.Client/HaushaltsPlaner.Client.csproj"
COPY . .
RUN dotnet publish "HaushaltsPlaner.Client/HaushaltsPlaner.Client.csproj" -c Release -o /app/publish -p:WasmBuildNative=false -p:RunAOTCompilation=false

FROM nginx:alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY HaushaltsPlaner.Client/nginx.conf /etc/nginx/conf.d/default.conf
RUN echo "{}" > /usr/share/nginx/html/appsettings.json
EXPOSE 80
