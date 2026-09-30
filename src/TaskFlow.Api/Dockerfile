# Build context: repository root (the API references src/TaskFlow.Shared).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes.
COPY global.json Directory.Build.props ./
COPY src/TaskFlow.Shared/TaskFlow.Shared.csproj src/TaskFlow.Shared/packages.lock.json src/TaskFlow.Shared/
COPY src/TaskFlow.Api/TaskFlow.Api.csproj src/TaskFlow.Api/packages.lock.json src/TaskFlow.Api/
RUN dotnet restore src/TaskFlow.Api/TaskFlow.Api.csproj --locked-mode

COPY src/TaskFlow.Shared/ src/TaskFlow.Shared/
COPY src/TaskFlow.Api/ src/TaskFlow.Api/
RUN dotnet publish src/TaskFlow.Api/TaskFlow.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Non-root user built into the .NET 8 images.
USER app
ENTRYPOINT ["dotnet", "TaskFlow.Api.dll"]
