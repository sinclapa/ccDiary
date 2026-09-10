# Dockerfile for ccDiary API - Uses pre-built artifacts
# Prerequisites: Build the application first using: dotnet publish ccDiaryApi/ccDiaryApi.csproj -c Release -o ./publish
# Then build the image: docker build -t ccdiary-api .

# Chiseled rather than bookworm-slim because the base image was ~87MB of the ~97MB pulled on
# every cold start, and the app scales to zero — so the pull is on the critical path for a
# real user, not just for deployment. Chiseled drops the shell and package manager, and the
# app sets InvariantGlobalization so the ICU layer goes too. There is no shell in here: debug
# with `az containerapp logs`, not `exec`.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled AS runtime

# Chiseled already runs as a non-root user; naming it keeps the intent obvious.
USER $APP_UID
WORKDIR /app

EXPOSE 8080

# Copy the pre-built application artifacts
# Expects the published output to be in ./publish directory relative to Dockerfile
COPY publish/ .

# No HEALTHCHECK: Container Apps ignores the Docker one, and the startup probe in
# deploy/containerApps.bicep is what actually gates ingress. The instruction that used to be
# here pointed at /health, which the app never mapped, and ran curl, which is not installed.

ENTRYPOINT ["dotnet", "ccDiaryApi.dll"]
