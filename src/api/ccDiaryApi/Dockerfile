#Force-9
#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src
ARG BUILD_CONFIGURATION=Release

# Copy solution and projects into image
COPY ["ccDiaryApi/ccDiaryApi.csproj", "ccDiaryApi/"]
COPY ["ccDiaryApiTest/ccDiaryApiTest.csproj", "ccDiaryApiTest/"]
RUN dotnet restore "./ccDiaryApi/ccDiaryApi.csproj"
RUN dotnet restore "./ccDiaryApiTest/ccDiaryApiTest.csproj"

# Copy full solution into image
COPY . .
RUN dotnet build "ccDiaryApi/ccDiaryApi.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore
RUN dotnet build "ccDiaryApiTest/ccDiaryApiTest.csproj" -c $BUILD_CONFIGURATION -o /app/test --no-restore

# run the unit tests
FROM build AS test
WORKDIR /src

# run the unit tests
RUN dotnet test "ccDiaryApiTest/ccDiaryApiTest.csproj" -c $BUILD_CONFIGURATION --no-build --logger "trx;LogFileName=testresults.trx" /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput="TestResults/coverage" /p:ExcludeByFile="**/*Migrations/*.cs" || exit 0

FROM scratch AS export-test-results
COPY --from=test /src/ccDiaryApiTest/TestResults/* .

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ccDiaryApi/ccDiaryApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ccDiaryApi.dll"]