FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY HappyHeadlines.wasm/HappyHeadlines.wasm.csproj HappyHeadlines.wasm/
COPY HappyHeadlinesApp/HappyHeadlinesApp.csproj HappyHeadlinesApp/

RUN dotnet restore HappyHeadlines.wasm/HappyHeadlines.wasm.csproj

COPY HappyHeadlines.wasm/ HappyHeadlines.wasm/
COPY HappyHeadlinesApp/ HappyHeadlinesApp/

RUN dotnet publish HappyHeadlines.wasm/HappyHeadlines.wasm.csproj \
    -c Release \
    -o /app/publish


FROM nginx:alpine AS final

COPY HappyHeadlines.wasm/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 80
