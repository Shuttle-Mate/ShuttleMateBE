# Sử dụng .NET SDK để build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /source

# Copy solution và project tương ứng với ShuttleMate
COPY ShuttleMate.sln .
COPY ./3.REPOSITORY/ShuttleMate.Contract.Repositories ./ShuttleMate.Contract.Repositories
COPY ./3.REPOSITORY/ShuttleMate.ModelViews ./ShuttleMate.ModelViews
COPY ./3.REPOSITORY/ShuttleMate.Repositories ./ShuttleMate.Repositories
COPY ./4.CORE/ShuttleMate.Core ./ShuttleMate.Core
COPY ./2.SERVICE/ShuttleMate.Contract.Services ./ShuttleMate.Contract.Services
COPY ./2.SERVICE/ShuttleMate.Services ./ShuttleMate.Services
COPY ./1.SERVER/ShuttleMate.API ./ShuttleMate.API

# Restore dependencies
RUN dotnet restore

# Build và publish
WORKDIR /source/ShuttleMate.API
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish ./ShuttleMate.API.csproj --use-current-runtime --self-contained false -o /app

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Thêm globalization và timezone
RUN apk add --no-cache icu-libs tzdata \
    && ln -s /usr/lib/libicudata.so.73 /usr/lib/libicudata.so.66 \
    && ln -s /usr/lib/libicui18n.so.73 /usr/lib/libicui18n.so.66 \
    && ln -s /usr/lib/libicuuc.so.73 /usr/lib/libicuuc.so.66

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy app đã publish
COPY --from=build /app .

# Chạy bằng user không phải root
RUN adduser -D app \
    && chown -R app:app /app \
    && chmod -R 755 /app

USER app

ENTRYPOINT ["dotnet", "ShuttleMate.API.dll"]
