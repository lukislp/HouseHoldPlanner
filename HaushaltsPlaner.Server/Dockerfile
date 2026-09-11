FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:6a94333d37514e385650a3c81a55e5350b67253dbe136e9cf17e499c35606a8c AS base
WORKDIR /app
EXPOSE 5242

FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:2fa828c68761b1b8c23d7662dc134421b9d3b59fe1425fdbc80804e390cdb24d AS build
WORKDIR /src
COPY ["HaushaltsPlaner.Server/HaushaltsPlaner.Server.csproj", "HaushaltsPlaner.Server/"]
COPY ["HaushaltsPlaner.Shared/HaushaltsPlaner.Shared.csproj", "HaushaltsPlaner.Shared/"]
RUN dotnet restore "HaushaltsPlaner.Server/HaushaltsPlaner.Server.csproj"
COPY . .
RUN dotnet publish "HaushaltsPlaner.Server/HaushaltsPlaner.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
VOLUME ["/app/data"]
ENTRYPOINT ["dotnet", "HaushaltsPlaner.Server.dll"]
