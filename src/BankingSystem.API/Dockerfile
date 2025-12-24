# ====================================
# Stage 1: Build (Dockerfile nằm trong src/BankingSystem.API/)
# ====================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_VERSION=1.0.0
ARG BUILD_DATE
WORKDIR /src

# Copy solution file
COPY ["BankingSystem.sln", "./"]

# Copy all csproj files (including tests)
COPY ["src/BankingSystem.API/BankingSystem.API.csproj", "src/BankingSystem.API/"]
COPY ["src/BankingSystem.Application/BankingSystem.Application.csproj", "src/BankingSystem.Application/"]
COPY ["src/BankingSystem.Domain/BankingSystem.Domain.csproj", "src/BankingSystem.Domain/"]
COPY ["src/BankingSystem.Infrastructure/BankingSystem.Infrastructure.csproj", "src/BankingSystem.Infrastructure/"]
COPY ["tests/BankingSystem.Tests/BankingSystem.Tests.csproj", "tests/BankingSystem.Tests/"]
COPY ["tests/BankingSystem.IntegrationTests/BankingSystem.IntegrationTests.csproj", "tests/BankingSystem.IntegrationTests/"]

# Restore dependencies
RUN dotnet restore "BankingSystem.sln"

# Copy everything else
COPY ["src/", "src/"]
COPY ["tests/", "tests/"]

# Build (without 'v' prefix in version - NuGet requires SemVer format like 1.0.0, not v1.0)
RUN dotnet build "BankingSystem.sln" \
    -c Release \
    --no-restore \
    /p:Version=${BUILD_VERSION}

# ====================================
# Stage 2: Publish
# ====================================
FROM build AS publish
ARG BUILD_VERSION=1.0.0

RUN dotnet publish "src/BankingSystem.API/BankingSystem.API.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:Version=${BUILD_VERSION}

# ====================================
# Stage 3: Runtime (Production)
# ====================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
ARG BUILD_VERSION=1.0.0
ARG BUILD_DATE

# Metadata
LABEL maintainer="BankingSystem Team" \
      version="${BUILD_VERSION}" \
      description="Banking System API - Production Image" \
      build.date="${BUILD_DATE}"

# Create non-root user for security
RUN groupadd -r bankingapp && useradd -r -g bankingapp bankingapp

WORKDIR /app

# Install required tools
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        curl \
        ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R bankingapp:bankingapp /app

# Switch to non-root user
USER bankingapp

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Expose port (non-privileged port)
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start application
ENTRYPOINT ["dotnet", "BankingSystem.API.dll"]