FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/Reader.web.wasm/Reader.web.wasm.csproj Reader.web.wasm/
COPY src/ReaderPages.web/ReaderPages.web.csproj ReaderPages.web/

RUN dotnet restore Reader.web.wasm/Reader.web.wasm.csproj

COPY src/Reader.web.wasm/ Reader.web.wasm/
COPY src/ReaderPages.web/ ReaderPages.web/

RUN dotnet publish Reader.web.wasm/Reader.web.wasm.csproj \
    -c Release \
    -o /app/publish

FROM nginx:alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 8001
