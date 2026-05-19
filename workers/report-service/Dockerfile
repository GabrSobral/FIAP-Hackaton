FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY src/ src/
COPY workers/report-service/ workers/report-service/
RUN dotnet restore workers/report-service/report-service.csproj
RUN dotnet publish workers/report-service/report-service.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "report-service.dll"]
