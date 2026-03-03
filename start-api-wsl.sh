#!/bin/bash

echo "=== ConsignmentGenie API Startup (WSL) ==="
echo "Using User Secrets for configuration..."
echo

# Navigate to API directory
cd /mnt/c/Projects/ConsignmentGenie/ConsignmentGenie_API/ConsignmentGenie.API

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET is not installed in WSL or not in PATH"
    echo "Please install .NET in WSL or use Windows dotnet"
    read -p "Press any key to exit..."
    exit 1
fi

echo "Cleaning build artifacts..."
find .. -name "bin" -type d -exec rm -rf {} + 2>/dev/null
find .. -name "obj" -type d -exec rm -rf {} + 2>/dev/null
echo "Build artifacts cleaned."
echo

echo "Building API..."
dotnet build
if [ $? -ne 0 ]; then
    echo "Build failed!"
    read -p "Press any key to exit..."
    exit 1
fi
echo

echo "Starting API server with User Secrets..."
echo "API will be available at: https://localhost:7042"
echo "Press Ctrl+C to stop"
echo
dotnet run
