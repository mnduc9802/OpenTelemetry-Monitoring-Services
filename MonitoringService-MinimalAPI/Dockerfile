# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy .csproj file and restore dependencies
COPY MonitoringService-MinimalAPI.csproj ./
RUN dotnet restore

# Copy the rest of the application and build
COPY . ./
RUN dotnet publish -c Release -o /app/out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

# Expose the port your API listens on (optional, if you want to map it in docker-compose)
EXPOSE 5015

ENTRYPOINT ["dotnet", "MonitoringService-MinimalAPI.dll"]
