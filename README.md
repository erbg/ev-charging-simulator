# EV Charging Point OCPP Simulator

A comprehensive .NET 9 OCPP (Open Charge Point Protocol) 1.6 simulator for electric vehicle charging stations. This project provides both automated and interactive simulation modes for testing and developing OCPP-compliant charging infrastructure without requiring physical hardware.

## Features

- **Full OCPP 1.6 Protocol Support** - Complete implementation including BootNotification, Authorize, StartTransaction, StopTransaction, MeterValues, and RemoteStart/Stop operations
- **Dual Operating Modes**:
  - **Automated Mode** - Background service for continuous testing and CI/CD integration
  - **Interactive Mode** - Console-based manual control for debugging and step-by-step testing
- **WebSocket Communication** - Real-time OCPP message exchange with charging management systems
- **Configurable Parameters** - Customizable charging point details, heartbeat intervals, and RFID cards
- **Realistic Simulation** - Simulates actual charging station behavior including meter values, status notifications, and transaction lifecycle
- **Multiple Charging Point Support** - Run multiple simulators simultaneously for load testing

## Project Structure

```
├── ecogy.app.chargepoint.simulator/     # Core simulator (Automated Mode)
│   ├── ChargingPointSimulator.cs        # Main simulator implementation
│   ├── ChargingPointConfiguration.cs    # Configuration classes
│   └── Program.cs                       # Automated mode entry point
│
├── ecogy.app.chargepoint.interactive/   # Interactive Mode
│   ├── Program.cs                       # Interactive mode entry point
│   └── InteractiveSimulator.cs          # Console interface
│
└── README.md                            # This file
```

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An OCPP 1.6 compliant central system/server to connect to

## Quick Start

### Option 1: Automated Mode (Background Service)

```bash
cd ecogy.app.chargepoint.simulator
dotnet run -- "ws://localhost:5000/ocpp" "CP001" "abc123"
```

### Option 2: Interactive Mode (Manual Control)

```bash
cd ecogy.app.chargepoint.interactive
dotnet run -- "ws://localhost:5000/ocpp" "CP001" "abc123"
```

### Command Line Arguments

```
Usage: dotnet run -- <serverUrl> <chargePointId> <testRfidCard>

Parameters:
  serverUrl      - WebSocket URL of your OCPP server
  chargePointId  - Unique identifier for this charging point
  testRfidCard   - RFID card ID for testing transactions

Example:
  dotnet run -- "ws://localhost:5000/ocpp/websocket/CentralSystemService" "CP001" "abc123"
```

## Usage Examples

### 1. Development & Testing

Start the interactive simulator for manual testing:

```bash
cd ecogy.app.chargepoint.interactive
dotnet run -- "ws://localhost:5000/ocpp" "DEV_CP001" "dev123"
```

Available interactive commands:
- `1` - Authorize RFID card
- `2` - Simulate complete transaction (Authorize → Start → MeterValues → Stop)
- `3` - Change connector status
- `4` - Show current configuration
- `h` - Show help
- `q` - Quit

### 2. Load Testing (Multiple Charging Points)

Run multiple simulators simultaneously:

```bash
# Terminal 1
cd ecogy.app.chargepoint.simulator
dotnet run -- "ws://localhost:5000/ocpp" "CP001" "card001" &

# Terminal 2
dotnet run -- "ws://localhost:5000/ocpp" "CP002" "card002" &

# Terminal 3 - Interactive control
cd ecogy.app.chargepoint.interactive
dotnet run -- "ws://localhost:5000/ocpp" "CP003" "card003"
```

### 3. API Integration Testing

Test RemoteStartTransaction and RemoteStopTransaction commands:

```bash
# Start simulator
dotnet run -- "ws://localhost:5000/ocpp" "CP001" "abc123"

# In another terminal, call your API
curl -X POST "https://localhost:5001/api/charging/remote-start" \
  -H "Content-Type: application/json" \
  -d '{"chargePointId": "CP001", "idTag": "abc123", "connectorId": 1}'
```

## Configuration

The simulator can be configured through the `ChargingPointConfiguration` class:

```csharp
var config = new ChargingPointConfiguration
{
    ServerUrl = "ws://localhost:5000/ocpp",
    ChargePointId = "CP001",
    ChargePointModel = "ECO-DC-50",
    ChargePointVendor = "Ecogy",
    HeartbeatInterval = 30,                    // seconds
    TestRfidCard = "abc123"
};
```

## OCPP Protocol Flow

### Connection & Registration
1. WebSocket connection to OCPP server
2. BootNotification message
3. Periodic Heartbeat messages
4. StatusNotification for connector states

### Transaction Flow
1. **Authorize** - Validate RFID card
2. **StartTransaction** - Begin charging session
3. **MeterValues** - Send periodic energy consumption data
4. **StopTransaction** - End charging session

### Remote Commands (Server → Charging Point)
- **RemoteStartTransaction** - Server initiates charging
- **RemoteStopTransaction** - Server stops charging
- **GetConfiguration** - Retrieve charging point settings
- **ChangeConfiguration** - Update charging point settings
- **Reset** - Restart charging point

## Use Cases

1. **API Development** - Test your OCPP central system implementation
2. **Integration Testing** - Automated test scenarios for CI/CD pipelines
3. **Load Testing** - Simulate multiple charging points simultaneously
4. **Protocol Debugging** - Interactive message-by-message OCPP communication analysis
5. **Training** - Learn OCPP protocol behavior and message flows

## Debugging

### Enable Detailed Logging

All WebSocket messages are logged with timestamps:

```
[12:34:56.789] Sent WebSocket message: [2,"20241201123456789","StartTransaction",{"connectorId":1,"idTag":"abc123","meterStart":147,"timestamp":"2024-12-01T12:34:56.789Z"}]
[12:34:56.890] Received WebSocket message: [3,"20241201123456789",{"transactionId":123,"idTagInfo":{"status":"Accepted"}}]
```

### Common Issues

1. **Connection Failed**
   - Verify OCPP server is running
   - Check WebSocket URL format
   - Ensure server supports "ocpp1.6" subprotocol

2. **Authorization Failed**
   - Verify RFID card is configured in your system
   - Check server logs for validation errors

3. **Transaction Issues**
   - Ensure StartTransaction completes before sending MeterValues
   - Verify transaction ID matching between Start/Stop

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Related Projects

- [OCPP 1.6 Specification](https://www.openchargealliance.org/protocols/ocpp-16/)
- [OCPP 2.0.1 Specification](https://www.openchargealliance.org/protocols/ocpp-201/)

## Support

For questions or issues, please open an issue on GitHub or contact the development team.

---

**Built with love for the EV charging ecosystem**