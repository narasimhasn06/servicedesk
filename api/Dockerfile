# ---- Build stage: has the full .NET SDK ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (this layer is cached until the .csproj changes)
COPY src/ServiceDesk.Api/ServiceDesk.Api.csproj src/ServiceDesk.Api/
RUN dotnet restore src/ServiceDesk.Api/ServiceDesk.Api.csproj

# Then copy the code and publish a Release build
COPY src/ src/
RUN dotnet publish src/ServiceDesk.Api/ServiceDesk.Api.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage: small image, no SDK, no source code ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Run as the image's built-in non-root user (safer)
USER $APP_UID
ENTRYPOINT ["dotnet", "ServiceDesk.Api.dll"]