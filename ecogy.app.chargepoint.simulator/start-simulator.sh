#!/bin/bash

# Charging Point Simulator Start Script

echo "=== OCPP Charging Point Simulator ==="
echo ""

# Default values
SERVER_URL="ws://localhost:5000/ocpp"
CHARGE_POINT_ID="CP001"
RFID_CARD="abc123"

# Check if parameters were provided
if [ $# -ge 1 ]; then
    SERVER_URL=$1
fi

if [ $# -ge 2 ]; then
    CHARGE_POINT_ID=$2
fi

if [ $# -ge 3 ]; then
    RFID_CARD=$3
fi

echo "Configuration:"
echo "  Server URL: $SERVER_URL"
echo "  Charging Point ID: $CHARGE_POINT_ID"
echo "  Test RFID Card: $RFID_CARD"
echo ""

# Ask user which mode to run
echo "Select simulation mode:"
echo "  1) Automatic simulation (runs in background)"
echo "  2) Interactive simulation (manual control)"
echo ""
read -p "Enter your choice (1 or 2): " MODE

case $MODE in
    1)
        echo "Starting automatic simulation..."
        dotnet run --project ecogy.app.chargepoint.simulator.csproj -- "$SERVER_URL" "$CHARGE_POINT_ID" "$RFID_CARD"
        ;;
    2)
        echo "Starting interactive simulation..."
        echo "Note: You can manually control the charging point using commands."
        echo "Type 'h' for help once the simulator starts."
        echo ""
        dotnet run --project ../ecogy.app.chargepoint.interactive/ecogy.app.chargepoint.interactive.csproj -- "$SERVER_URL" "$CHARGE_POINT_ID" "$RFID_CARD"
        ;;
    *)
        echo "Invalid choice. Please run the script again and select 1 or 2."
        exit 1
        ;;
esac