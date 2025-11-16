#!/bin/bash
set -e

# Function to handle shutdown
cleanup() {
    echo "Shutting down..."
    if [ ! -z "$SERVER_PID" ]; then
        kill $SERVER_PID 2>/dev/null || true
        wait $SERVER_PID 2>/dev/null || true
    fi
    if [ ! -z "$CLIENT_PID" ]; then
        kill $CLIENT_PID 2>/dev/null || true
        wait $CLIENT_PID 2>/dev/null || true
    fi
    exit 0
}

# Set up signal handlers
trap cleanup SIGTERM SIGINT

# Check if we should run client dev server
# Run dev server if: Development mode OR if CLIENT_DEV_MODE env var is set to "true"
SHOULD_RUN_CLIENT_DEV=false
if [ "$ASPNETCORE_ENVIRONMENT" = "Development" ]; then
    SHOULD_RUN_CLIENT_DEV=true
fi
if [ "$CLIENT_DEV_MODE" = "true" ]; then
    SHOULD_RUN_CLIENT_DEV=true
fi

if [ "$SHOULD_RUN_CLIENT_DEV" = "true" ] && [ -d "src/client" ] && command -v npm &> /dev/null; then
    echo "Starting in DEVELOPMENT mode..."
    echo "Server will run on https://localhost:5001"
    echo "Client dev server will run on http://localhost:5173"
    echo ""
    
    # Install client dependencies if node_modules doesn't exist
    if [ ! -d "src/client/node_modules" ]; then
        echo "Installing client dependencies..."
        cd src/client
        npm install
        cd ../..
    fi
    
    # Start the client dev server in the background
    # Output will go to stdout/stderr naturally - no redirection needed
    cd src/client
    npm run dev &
    CLIENT_PID=$!
    cd ../..
    
    # Give client a moment to start and show initial output
    sleep 1
    
    echo "=== Starting Server ==="
    echo ""
    
    # Start the server in the background as well
    # Both processes will output to the same stdout/stderr
    dotnet server.dll &
    SERVER_PID=$!
    
    echo "Both processes are running:"
    echo "  - Server PID: $SERVER_PID"
    echo "  - Client PID: $CLIENT_PID"
    echo ""
    echo "Output from both processes will appear below:"
    echo "=========================================="
    echo ""
    
    # Wait for both processes - output will be interleaved
    # Use wait with timeout to periodically check if processes are still running
    while true; do
        # Check if server is still running
        if ! kill -0 $SERVER_PID 2>/dev/null; then
            SERVER_EXIT=$?
            echo "Server process exited with code: $SERVER_EXIT"
            break
        fi
        # Check if client is still running
        if ! kill -0 $CLIENT_PID 2>/dev/null; then
            CLIENT_EXIT=$?
            echo "Client process exited with code: $CLIENT_EXIT"
            break
        fi
        sleep 1
    done
    
    # Clean up remaining process
    if kill -0 $SERVER_PID 2>/dev/null; then
        echo "Stopping server..."
        kill $SERVER_PID 2>/dev/null || true
        wait $SERVER_PID 2>/dev/null || true
    fi
    if kill -0 $CLIENT_PID 2>/dev/null; then
        echo "Stopping client dev server..."
        kill $CLIENT_PID 2>/dev/null || true
        wait $CLIENT_PID 2>/dev/null || true
    fi
    
    exit ${SERVER_EXIT:-0}
else
    echo "Starting in PRODUCTION mode..."
    echo "Server will serve the built client from wwwroot"
    
    # In production, use exec to replace the shell process with dotnet
    # This ensures proper signal handling (SIGTERM, SIGINT)
    exec dotnet server.dll
fi
