# ----------------------------------------------------
# Multi-stage Dockerfile for QA and Production Builds
# ----------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files
COPY ["src/Listed.API/Listed.API.csproj", "src/Listed.API/"]
COPY ["src/Listed.Domain/Listed.Domain.csproj", "src/Listed.Domain/"]
COPY ["src/Listed.Application/Listed.Application.csproj", "src/Listed.Application/"]
COPY ["src/Listed.Infrastructure/Listed.Infrastructure.csproj", "src/Listed.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/Listed.API/Listed.API.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/Listed.API"
RUN dotnet publish "Listed.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001
EXPOSE 5000
EXPOSE 5001
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Listed.API.dll"]
