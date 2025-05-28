# Sử dụng .NET SDK để build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /source

# Copy solution và project tương ứng với ShuttleMate
COPY ShuttleMateBE.sln .
COPY ./ShuttleMate.Contract.Repositories ./ShuttleMate.Contract.Repositories
COPY ./ShuttleMate.ModelViews ./ShuttleMate.ModelViews
COPY ./ShuttleMate.Repositories ./ShuttleMate.Repositories
COPY ./ShuttleMate.Core ./ShuttleMate.Core
COPY ./ShuttleMate.Contract.Services ./ShuttleMate.Contract.Services
COPY ./ShuttleMate.Services ./ShuttleMate.Services
COPY ./ShuttleMate.API ./ShuttleMate.API

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


USER app

ENTRYPOINT ["dotnet", "ShuttleMate.API.dll"]
