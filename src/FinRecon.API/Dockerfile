# Build context must be ./src (the solution root)
# so project references to Core and Infrastructure resolve correctly.
# Usage in docker-compose: context: ./src, dockerfile: FinRecon.API/Dockerfile

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FinRecon.sln .
COPY FinRecon.Core/FinRecon.Core.csproj FinRecon.Core/
COPY FinRecon.Infrastructure/FinRecon.Infrastructure.csproj FinRecon.Infrastructure/
COPY FinRecon.API/FinRecon.API.csproj FinRecon.API/
COPY FinRecon.Worker/FinRecon.Worker.csproj FinRecon.Worker/
COPY FinRecon.Tests/FinRecon.Tests.csproj FinRecon.Tests/

RUN dotnet restore FinRecon.sln

COPY . .

RUN dotnet publish FinRecon.API/FinRecon.API.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "FinRecon.API.dll"]
