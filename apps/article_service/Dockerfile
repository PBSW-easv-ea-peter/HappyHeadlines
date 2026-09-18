FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ArticleService/ArticleService.csproj", "."]
RUN dotnet restore

COPY ["./src/ArticleService/", "."]
RUN dotnet publish -c Release -o /app

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ArticleService.dll"]