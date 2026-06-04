# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["PennyWise.csproj", "./"]
RUN dotnet restore

# Copy all files and build/publish in Release mode (disable native AppHost)
COPY . .
RUN dotnet publish -c Release -o /app /p:UseAppHost=false

# Run stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

# Expose port 8080 (standard for .NET 8 containers)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Environment variables for low-resource environments (Render Free Tier)
# 1. Disable Server GC to save memory (Workstation GC is much lighter)
ENV DOTNET_gcServer=0
# 2. Enable globalization invariant mode to prevent ICU-related segfaults
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

ENTRYPOINT ["dotnet", "PennyWise.dll"]
