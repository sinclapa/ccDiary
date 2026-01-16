# Dockerfile for ccDiary API - Uses pre-built artifacts
# Prerequisites: Build the application first using: dotnet publish ccDiaryApi/ccDiaryApi.csproj -c Release -o ./publish
# Then build the image: docker build -t ccdiary-api .

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime

# Create app user
USER $APP_UID
WORKDIR /app

# Expose ports
EXPOSE 8080
EXPOSE 443

# Copy the pre-built application artifacts
# Expects the published output to be in ./publish directory relative to Dockerfile
COPY publish/ .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ccDiaryApi.dll"]