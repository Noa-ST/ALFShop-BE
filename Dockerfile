# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish eCommerceApp.Host/eCommerceApp.Host.csproj -c Release -o /app

# Run
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
# Kestrel sẽ lắng nghe theo ASPNETCORE_URLS do Render cấp (http://0.0.0.0:${PORT})
ENTRYPOINT ["dotnet", "eCommerceApp.Host.dll"]