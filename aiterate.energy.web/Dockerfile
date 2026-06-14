FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY aiterate.energy.web.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app
EXPOSE 3000
ENV ASPNETCORE_URLS=http://+:3000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "aiterate.energy.web.dll"]
