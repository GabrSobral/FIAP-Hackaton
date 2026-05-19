FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY src/ src/
COPY workers/analysis-worker/ workers/analysis-worker/
RUN dotnet restore workers/analysis-worker/analysis-worker.csproj
RUN dotnet publish workers/analysis-worker/analysis-worker.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "analysis-worker.dll"]
