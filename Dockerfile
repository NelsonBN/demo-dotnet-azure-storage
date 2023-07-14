FROM mcr.microsoft.com/dotnet/sdk:8.0.100-preview.6-bookworm-slim AS build-env

EXPOSE 80
WORKDIR /src



# Copy application (API)
COPY ./src/Demo.Api/*.csproj /src/Demo.Api/

# Restore nuget packages
RUN dotnet restore /src/Demo.Api/*.csproj

# Copy all the source code and build
COPY ./src /src

# Build and publish the application. Used the "--no-restore" and "--no-build" to benefit the layer caches
RUN dotnet build --configuration Release /src/Demo.Api/*.csproj
RUN dotnet publish /src/Demo.Api/*.csproj --configuration Release --no-build --no-restore -o /app



FROM mcr.microsoft.com/dotnet/aspnet:8.0.0-preview.6-bookworm-slim AS final-env

WORKDIR /app

# Froce application to run in Production
ENV ASPNETCORE_ENVIRONMENT Production
ENV ASPNETCORE_URLS http://*:80

COPY --from=build-env /app .

ENTRYPOINT ["dotnet", "Demo.Api.dll"]