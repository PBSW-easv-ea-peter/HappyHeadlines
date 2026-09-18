FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/HappyHeadlines.web.wasm/HappyHeadlines.web.wasm.csproj HappyHeadlines.web.wasm/
COPY src/HappyHeadlinesPages.web/HappyHeadlinesPages.web.csproj HappyHeadlinesPages.web/

RUN dotnet restore HappyHeadlines.web.wasm/HappyHeadlines.web.wasm.csproj

COPY src/HappyHeadlines.web.wasm/ HappyHeadlines.web.wasm/
COPY src/HappyHeadlinesPages.web/ HappyHeadlinesPages.web/

RUN dotnet publish HappyHeadlines.web.wasm/HappyHeadlines.web.wasm.csproj \
    -c Release \
    -o /app/publish


FROM nginx:alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 8000
