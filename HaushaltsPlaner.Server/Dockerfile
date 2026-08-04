FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5242

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
