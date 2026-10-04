# Build context is ./apps (see docker-compose) so the shared libraries are reachable.
# Folder layout is kept so relative ProjectReferences resolve inside the container.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["shared/HappyHeadlines.Observability/HappyHeadlines.Observability.csproj", "shared/HappyHeadlines.Observability/"]
COPY ["shared/src/Messaging/Messaging.csproj", "shared/src/Messaging/"]
COPY ["publish_service/src/PublishService/PublishService.csproj", "publish_service/src/PublishService/"]
RUN dotnet restore "publish_service/src/PublishService/PublishService.csproj"

COPY ["shared/HappyHeadlines.Observability/", "shared/HappyHeadlines.Observability/"]
COPY ["shared/src/Messaging/", "shared/src/Messaging/"]
COPY ["publish_service/src/PublishService/", "publish_service/src/PublishService/"]
RUN dotnet publish "publish_service/src/PublishService/PublishService.csproj" -c Release -o /app

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "PublishService.dll"]
