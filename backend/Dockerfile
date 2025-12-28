# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY ["src/PropertyManager.Domain/PropertyManager.Domain.csproj", "src/PropertyManager.Domain/"]
COPY ["src/PropertyManager.Application/PropertyManager.Application.csproj", "src/PropertyManager.Application/"]
COPY ["src/PropertyManager.Infrastructure/PropertyManager.Infrastructure.csproj", "src/PropertyManager.Infrastructure/"]
COPY ["src/PropertyManager.Api/PropertyManager.Api.csproj", "src/PropertyManager.Api/"]

# Restore dependencies (this layer is cached if project files don't change)
RUN dotnet restore "src/PropertyManager.Api/PropertyManager.Api.csproj"

# Copy remaining source code (exclude test projects via .dockerignore)
COPY src/ src/

# Build and publish
RUN dotnet publish "src/PropertyManager.Api/PropertyManager.Api.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security (Debian-based image)
RUN useradd --create-home --shell /bin/bash appuser

# Copy published output
COPY --from=build /app/publish .

# Set ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check (using wget which is available in the base image)
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/api/v1/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "PropertyManager.Api.dll"]
