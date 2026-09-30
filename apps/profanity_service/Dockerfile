# Build context is ./apps (see docker-compose) so the shared library is reachable.
# Folder layout is kept so relative ProjectReferences resolve inside the container.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["shared/HappyHeadlines.Observability/HappyHeadlines.Observability.csproj", "shared/HappyHeadlines.Observability/"]
COPY ["profanity_service/src/ProfanityService/ProfanityService.csproj", "profanity_service/src/ProfanityService/"]
RUN dotnet restore "profanity_service/src/ProfanityService/ProfanityService.csproj"

COPY ["shared/HappyHeadlines.Observability/", "shared/HappyHeadlines.Observability/"]
COPY ["profanity_service/src/ProfanityService/", "profanity_service/src/ProfanityService/"]
RUN dotnet publish "profanity_service/src/ProfanityService/ProfanityService.csproj" -c Release -o /app

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ProfanityService.dll"]
