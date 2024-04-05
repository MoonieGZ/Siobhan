FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Siobhan.csproj", "."]
RUN dotnet restore "./Siobhan.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Siobhan.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Siobhan.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
ENV \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8
RUN apk add --no-cache \
    icu-data-full \
    icu-libs
RUN apk add --update \
    curl \
    && rm -rf /var/cache/apk/*

WORKDIR /app
COPY --from=publish /app/publish .
RUN chown -R 1000:1000 /app

USER 1000
ENTRYPOINT ["dotnet", "Siobhan.dll"]

HEALTHCHECK --interval=60s --retries=5 CMD curl --fail http://localhost:8080/health || exit 1