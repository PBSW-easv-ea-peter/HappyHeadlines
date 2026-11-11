FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["shared/HappyHeadlines.Observability/HappyHeadlines.Observability.csproj", "/shared/HappyHeadlines.Observability/"]
COPY ["shared/src/Messaging/Messaging.csproj", "/shared/src/Messaging/"]
COPY ["newsletter_service/src/NewsletterService/NewsletterService.csproj", "src/NewsletterService/"]
RUN dotnet restore "src/NewsletterService/NewsletterService.csproj"

COPY ["shared/HappyHeadlines.Observability/", "/shared/HappyHeadlines.Observability/"]
COPY ["shared/src/Messaging/", "/shared/src/Messaging/"]
COPY ["newsletter_service/src/NewsletterService/", "src/NewsletterService/"]
RUN dotnet publish "src/NewsletterService/NewsletterService.csproj" -c Release -o /app

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "NewsletterService.dll"]

