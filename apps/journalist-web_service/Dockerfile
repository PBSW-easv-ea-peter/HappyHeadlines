FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/Journalist.web.wasm/Journalist.web.wasm.csproj Journalist.web.wasm/
COPY src/JournalistPages.web/JournalistPages.web.csproj JournalistPages.web/

RUN dotnet restore Journalist.web.wasm/Journalist.web.wasm.csproj

COPY src/Journalist.web.wasm/ Journalist.web.wasm/
COPY src/JournalistPages.web/ JournalistPages.web/

RUN dotnet publish Journalist.web.wasm/Journalist.web.wasm.csproj \
    -c Release \
    -o /app/publish


FROM nginx:alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Override the base appsettings.json with production values
COPY --from=build /app/publish/wwwroot/appsettings.Production.json /usr/share/nginx/html/appsettings.json

# Production file is now baked into appsettings.json — drop the duplicate
#RUN rm -f /usr/share/nginx/html/appsettings.Production.json

EXPOSE 8000
