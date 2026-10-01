# Build context is ./apps (see docker-compose) so the shared libraries are reachable.
# Folder layout is kept so relative ProjectReferences resolve inside the container.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["shared/HappyHeadlines.Observability/HappyHeadlines.Observability.csproj", "shared/HappyHeadlines.Observability/"]
COPY ["shared/src/Messaging/Messaging.csproj", "shared/src/Messaging/"]
COPY ["article_service/src/ArticleService/ArticleService.csproj", "article_service/src/ArticleService/"]
RUN dotnet restore "article_service/src/ArticleService/ArticleService.csproj"

COPY ["shared/HappyHeadlines.Observability/", "shared/HappyHeadlines.Observability/"]
COPY ["shared/src/Messaging/", "shared/src/Messaging/"]
COPY ["article_service/src/ArticleService/", "article_service/src/ArticleService/"]
RUN dotnet publish "article_service/src/ArticleService/ArticleService.csproj" -c Release -o /app

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ArticleService.dll"]
