# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution file and all project files
COPY *.sln .
COPY ConsignmentGenie.API/*.csproj ./ConsignmentGenie.API/
COPY ConsignmentGenie.Core/*.csproj ./ConsignmentGenie.Core/
COPY ConsignmentGenie.Infrastructure/*.csproj ./ConsignmentGenie.Infrastructure/
COPY ConsignmentGenie.Application/*.csproj ./ConsignmentGenie.Application/
COPY ConsignmentGenie.Tests/*.csproj ./ConsignmentGenie.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

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