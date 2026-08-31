# ---- build -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY dotnet-start.csproj ./
RUN dotnet restore dotnet-start.csproj

COPY . .
RUN dotnet publish dotnet-start.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- run ---------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Blazor Server keeps a SignalR circuit per visitor, so the container must be
# long-lived — this is not a static bundle.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=1
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "dotnet-start.dll"]
