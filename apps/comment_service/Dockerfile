# Build context is ./apps (see docker-compose) so the shared library is reachable.
# Folder layout is kept so relative ProjectReferences resolve inside the container.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["shared/HappyHeadlines.Observability/HappyHeadlines.Observability.csproj", "shared/HappyHeadlines.Observability/"]
COPY ["comment_service/src/CommentService/CommentService.csproj", "comment_service/src/CommentService/"]
RUN dotnet restore "comment_service/src/CommentService/CommentService.csproj"

COPY ["shared/HappyHeadlines.Observability/", "shared/HappyHeadlines.Observability/"]
COPY ["comment_service/src/CommentService/", "comment_service/src/CommentService/"]
RUN dotnet publish "comment_service/src/CommentService/CommentService.csproj" -c Release -o /app

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "CommentService.dll"]
