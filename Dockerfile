# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution file and all project files
COPY ConsignmentGenie_API/*.sln .
COPY ConsignmentGenie_API/ConsignmentGenie.API/*.csproj ./ConsignmentGenie.API/
COPY ConsignmentGenie_API/ConsignmentGenie.Core/*.csproj ./ConsignmentGenie.Core/
COPY ConsignmentGenie_API/ConsignmentGenie.Infrastructure/*.csproj ./ConsignmentGenie.Infrastructure/
COPY ConsignmentGenie_API/ConsignmentGenie.Application/*.csproj ./ConsignmentGenie.Application/
COPY ConsignmentGenie_API/ConsignmentGenie.Tests/*.csproj ./ConsignmentGenie.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY ConsignmentGenie_API/ .

# Build and publish the application
RUN dotnet publish ConsignmentGenie.API/ConsignmentGenie.API.csproj -c Release -o /app/publish

# Use the official .NET 8 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published application
COPY --from=build /app/publish .

# Expose port 8080 (Render's default)
EXPOSE 8080

# Set environment variable for Render
ENV ASPNETCORE_URLS=http://+:8080

# Set the entry point
ENTRYPOINT ["dotnet", "ConsignmentGenie.API.dll"]