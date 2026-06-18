FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY aiterate.energy.collector/aiterate.energy.collector.csproj aiterate.energy.collector/
RUN dotnet restore aiterate.energy.collector/aiterate.energy.collector.csproj

COPY aiterate.energy.collector/ aiterate.energy.collector/
COPY aiterate.energy.web/ aiterate.energy.web/
RUN dotnet publish aiterate.energy.collector/aiterate.energy.collector.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "aiterate.energy.collector.dll"]
