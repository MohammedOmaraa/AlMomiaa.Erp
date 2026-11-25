# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY AlMomiaa.Erp.sln ./
COPY Directory.Build.props ./
COPY src/AlMomiaa.Domain/AlMomiaa.Domain.csproj src/AlMomiaa.Domain/
COPY src/AlMomiaa.Application/AlMomiaa.Application.csproj src/AlMomiaa.Application/
COPY src/AlMomiaa.Infrastructure/AlMomiaa.Infrastructure.csproj src/AlMomiaa.Infrastructure/
COPY src/AlMomiaa.WebApi/AlMomiaa.WebApi.csproj src/AlMomiaa.WebApi/

RUN dotnet restore src/AlMomiaa.WebApi/AlMomiaa.WebApi.csproj

COPY src/ src/

RUN dotnet publish src/AlMomiaa.WebApi/AlMomiaa.WebApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV DOTNET_EnableDiagnostics=0 \
    ASPNETCORE_ENVIRONMENT=Production \
    PORT=10000

COPY --from=build /app/publish ./

USER app
EXPOSE 10000

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-10000} exec dotnet AlMomiaa.WebApi.dll"]
