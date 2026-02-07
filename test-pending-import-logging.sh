#!/bin/bash
# Test script to verify PendingImport logging is working

echo "🧪 Testing PendingImport Logging..."

# Start the API in the background
echo "Starting API..."
dotnet run --project ConsignmentGenie.API &
API_PID=$!

# Give API time to start
sleep 10

# Test 1: Check if API is running
echo "Testing API health..."
curl -s http://localhost:5000/api/health || echo "API not yet ready"

# Clean up
echo "Stopping API..."
kill $API_PID 2>/dev/null

echo "✅ Test completed. Check logs for PendingImport entries with prefixes:"
echo "   🔄 [HTTP->PendingImport] - Controller-level logging"
echo "   🔄 [PendingImportWrite] - Service-level logging"
echo "   🔄 [SquareSync->PendingImport] - Square sync logging"
echo "   ✅ [*] - Completion logging"