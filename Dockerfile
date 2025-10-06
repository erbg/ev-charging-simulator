# Use the official .NET 9 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Use the official .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["ecogy.app.chargepoint.simulator/ecogy.app.chargepoint.simulator.csproj", "ecogy.app.chargepoint.simulator/"]
COPY ["ecogy.app.chargepoint.interactive/ecogy.app.chargepoint.interactive.csproj", "ecogy.app.chargepoint.interactive/"]

# Restore dependencies
RUN dotnet restore "ecogy.app.chargepoint.simulator/ecogy.app.chargepoint.simulator.csproj"
RUN dotnet restore "ecogy.app.chargepoint.interactive/ecogy.app.chargepoint.interactive.csproj"

# Copy source code
COPY . .

# Build the applications
WORKDIR "/src/ecogy.app.chargepoint.simulator"
RUN dotnet build "ecogy.app.chargepoint.simulator.csproj" -c Release -o /app/build/simulator

WORKDIR "/src/ecogy.app.chargepoint.interactive"
RUN dotnet build "ecogy.app.chargepoint.interactive.csproj" -c Release -o /app/build/interactive

# Publish the applications
WORKDIR "/src/ecogy.app.chargepoint.simulator"
RUN dotnet publish "ecogy.app.chargepoint.simulator.csproj" -c Release -o /app/publish/simulator --no-restore

WORKDIR "/src/ecogy.app.chargepoint.interactive"
RUN dotnet publish "ecogy.app.chargepoint.interactive.csproj" -c Release -o /app/publish/interactive --no-restore

# Final stage/image
FROM base AS final
WORKDIR /app

# Copy published applications
COPY --from=build /app/publish/simulator ./simulator
COPY --from=build /app/publish/interactive ./interactive

# Copy example configuration files
COPY --from=build /src/example-*.json ./
COPY --from=build /src/example-*.env ./

# Create a startup script that can run either mode
COPY <<EOF /app/entrypoint.sh
#!/bin/bash
set -e

# Default values
MODE=\${SIMULATOR_MODE:-automated}
CONFIG_SOURCE=\${CONFIG_SOURCE:-env}

echo "Starting EV Charging Point OCPP Simulator"
echo "Mode: \$MODE"
echo "Config Source: \$CONFIG_SOURCE"
echo "=================================="

# Determine which application to run
if [ "\$MODE" = "interactive" ]; then
    APP_DIR="./interactive"
    APP_DLL="ChargingPointInteractive.dll"
    echo "Running Interactive Mode"
else
    APP_DIR="./simulator"
    APP_DLL="ecogy.app.chargepoint.simulator.dll"
    echo "Running Automated Mode"
fi

# Build command line arguments based on configuration source
ARGS=""

case "\$CONFIG_SOURCE" in
    "env")
        if [ "\${MULTIPLE_CONFIGS:-false}" = "true" ]; then
            ARGS="--env --multiple \${ENV_PREFIX:-OCPP_}"
        else
            ARGS="--env \${ENV_PREFIX:-OCPP_}"
        fi
        ;;
    "json")
        if [ -z "\$CONFIG_FILE" ]; then
            echo "Error: CONFIG_FILE environment variable required when CONFIG_SOURCE=json"
            exit 1
        fi
        if [ "\${MULTIPLE_CONFIGS:-false}" = "true" ]; then
            ARGS="--config \$CONFIG_FILE --multiple"
        else
            ARGS="--config \$CONFIG_FILE"
        fi
        ;;
    "args")
        # Use traditional command line arguments
        ARGS="\${SERVER_URL:-ws://localhost:5000/ocpp} \${CHARGE_POINT_ID:-CP001} \${TEST_RFID_CARD:-abc123}"
        ;;
    *)
        echo "Invalid CONFIG_SOURCE: \$CONFIG_SOURCE. Must be 'env', 'json', or 'args'"
        exit 1
        ;;
esac

echo "Starting with arguments: \$ARGS"
echo "=================================="

# Start the application
cd "\$APP_DIR"
exec dotnet "\$APP_DLL" \$ARGS
EOF

# Make the script executable
RUN chmod +x /app/entrypoint.sh

# Set environment variables with defaults
ENV SIMULATOR_MODE=automated
ENV CONFIG_SOURCE=env
ENV ENV_PREFIX=OCPP_
ENV MULTIPLE_CONFIGS=false

# Default OCPP configuration
ENV OCPP_SERVER_URL=ws://localhost:5000/ocpp
ENV OCPP_CHARGE_POINT_ID=CP001
ENV OCPP_CHARGE_POINT_MODEL=ECO-DC-50
ENV OCPP_CHARGE_POINT_VENDOR=Ecogy
ENV OCPP_HEARTBEAT_INTERVAL=30
ENV OCPP_TEST_RFID_CARD=abc123
ENV OCPP_CONNECTOR_COUNT=1

# Expose no specific ports (WebSocket client, not server)
# But document the typical OCPP server port for reference
EXPOSE 8080

# Health check (optional - checks if we can resolve DNS)
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD ping -c 1 google.com > /dev/null || exit 1

# Use our entrypoint script
ENTRYPOINT ["/app/entrypoint.sh"]