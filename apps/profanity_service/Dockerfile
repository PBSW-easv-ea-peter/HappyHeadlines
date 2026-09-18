FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/ProfanityService/ProfanityService.csproj", "."]
RUN dotnet restore

COPY ["./src/ProfanityService/", "."]
RUN dotnet publish -c Release -o /app

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ProfanityService.dll"]