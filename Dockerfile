# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["UtilityTools.sln", "./"]
COPY ["src/UtilityTools.Domain/UtilityTools.Domain.csproj", "src/UtilityTools.Domain/"]
COPY ["src/UtilityTools.Application/UtilityTools.Application.csproj", "src/UtilityTools.Application/"]
COPY ["src/UtilityTools.Infrastructure/UtilityTools.Infrastructure.csproj", "src/UtilityTools.Infrastructure/"]
COPY ["src/UtilityTools.Api/UtilityTools.Api.csproj", "src/UtilityTools.Api/"]
COPY ["src/UtilityTools.Shared/UtilityTools.Shared.csproj", "src/UtilityTools.Shared/"]

# Restore dependencies
RUN dotnet restore "UtilityTools.sln"

# Copy all source files
COPY . .

# Build the solution
WORKDIR "/src/src/UtilityTools.Api"
RUN dotnet build "UtilityTools.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "UtilityTools.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install FFmpeg for video processing (if needed)
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=publish /app/publish .

# Create storage directory
RUN mkdir -p /app/storage

EXPOSE 8080

ENTRYPOINT ["dotnet", "UtilityTools.Api.dll"]

