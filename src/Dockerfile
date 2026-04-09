FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first (layer-cached as long as csproj doesn't change)
COPY ["fiap-hackaton.csproj", "."]
RUN dotnet restore

# Copy remaining source (test folder excluded via .dockerignore)
COPY . .
RUN dotnet publish fiap-hackaton.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "fiap-hackaton.dll"]
