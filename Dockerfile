# STAGE 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copy project files first for better caching
COPY ./*.csproj ./
RUN dotnet restore

# 2. Copy remaining source
COPY . .

# 3. Publish release build
RUN dotnet publish -c Release -o /app

# STAGE 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Create required directories
RUN mkdir -p /app/Config && \
    mkdir -p /data/models && \
    mkdir -p /data/input

# Copy build output from previous stage
COPY --from=build /app .

# Configure environment
ENV DOTNET_ENVIRONMENT=Production
ENV ModelSaveDirectory=/data/models
ENV ModelFilePrefix=savedmodel

# Entrypoint
ENTRYPOINT ["dotnet", "AnomalyDetectionApp.dll"]