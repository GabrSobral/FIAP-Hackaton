FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY services/api/ services/api/
COPY services/analysis-worker/ services/analysis-worker/
RUN dotnet restore services/analysis-worker/analysis-worker.csproj
RUN dotnet publish services/analysis-worker/analysis-worker.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "analysis-worker.dll"]
