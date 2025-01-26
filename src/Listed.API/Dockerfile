# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Create a group and user (replace 1001 with any non-root UID)
RUN addgroup --system appgroup && adduser --system --ingroup appgroup --uid 1001 appuser

USER appuser
WORKDIR /app

# Listen on port 8080 and 8081
ENV ASPNETCORE_URLS="http://+:8080;http://+:8081"

# Expose both ports (for reference/documentation; not strictly required)
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Listed.API/Listed.API.csproj", "Listed.API/"]
RUN dotnet restore "./Listed.API/Listed.API.csproj"
COPY . .
WORKDIR "/src/Listed.API"
RUN dotnet build "./Listed.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Listed.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Listed.API.dll"]