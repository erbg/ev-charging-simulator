# EV Charging Point OCPP Simulator

A comprehensive .NET 9 OCPP (Open Charge Point Protocol) 1.6 simulator for electric vehicle charging stations. This project provides both automated and interactive simulation modes for testing and developing OCPP-compliant charging infrastructure without requiring physical hardware.

## Features

- **Full OCPP 1.6 Protocol Support** - Complete implementation including BootNotification, Authorize, StartTransaction, StopTransaction, MeterValues, and RemoteStart/Stop operations
- **Dual Operating Modes**:
  - **Automated Mode** - Background service for continuous testing and CI/CD integration
  - **Interactive Mode** - Console-based manual control for debugging and step-by-step testing
- **WebSocket Communication** - Real-time OCPP message exchange with charging management systems
- **Automatic Reconnection** - Robust connection handling with exponential backoff retry logic
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

**Command Line Arguments:**
```bash
cd ecogy.app.chargepoint.simulator
dotnet run -- "ws://localhost:5000/ocpp" "CP001" "abc123"
```

**Environment Variables:**
```bash
# Set environment variables
export OCPP_SERVER_URL="ws://localhost:5000/ocpp"
export OCPP_CHARGE_POINT_ID="CP001"
export OCPP_TEST_RFID_CARD="abc123"

# Run with environment variables
dotnet run -- --env
```

**JSON Configuration:**
```bash
# Create config.json file
dotnet run -- --config config.json
```

### Option 2: Interactive Mode (Manual Control)

**Command Line Arguments:**
```bash
cd ecogy.app.chargepoint.interactive
dotnet run -- "ws://localhost:5000/ocpp" "CP001" "abc123"
```

**Environment Variables:**
```bash
# Set environment variables (same as above)
dotnet run -- --env
```

**JSON Configuration:**
```bash
dotnet run -- --config config.json
```

### Option 3: Multiple Configurations

**Environment Variables (Multiple):**
```bash
# Set multiple configurations
export MULTIPLESUPPORT="true"

export OCPP_1_SERVER_URL="ws://localhost:5000/ocpp"
export OCPP_1_CHARGE_POINT_ID="CP001"
export OCPP_1_TEST_RFID_CARD="abc123"

export OCPP_2_SERVER_URL="ws://localhost:5000/ocpp"
export OCPP_2_CHARGE_POINT_ID="CP002"
export OCPP_2_TEST_RFID_CARD="def456"

# Automated mode - runs all simultaneously
cd ecogy.app.chargepoint.simulator
dotnet run -- --env --multiple

# Interactive mode - choose which to use
cd ecogy.app.chargepoint.interactive
dotnet run -- --env --multiple
```

**JSON Configuration (Multiple):**
```bash
# Automated mode - runs all simultaneously
dotnet run -- --config multiple-config.json --multiple

# Interactive mode - choose which to use
dotnet run -- --config multiple-config.json --multiple
```

### Command Line Arguments

**Single Configuration:**
```
Usage: dotnet run -- <serverUrl> <chargePointId> <testRfidCard>

Parameters:
  serverUrl      - WebSocket URL of your OCPP server
  chargePointId  - Unique identifier for this charging point
  testRfidCard   - default RFID card ID for testing transactions

Example:
  dotnet run -- "ws://localhost:5000/ocpp/websocket/CentralSystemService" "CP001" "abc123"
```

**Environment Variables:**
```
Usage: dotnet run -- --env [prefix] [--multiple]

Parameters:
  --env          - Load configuration from environment variables
  prefix         - Environment variable prefix (default: OCPP_)
  --multiple     - Load multiple configurations (numbered prefixes)

Examples:
  dotnet run -- --env                    # Uses OCPP_ prefix
  dotnet run -- --env CHARGER_           # Uses CHARGER_ prefix
  dotnet run -- --env --multiple         # Loads OCPP_1_, OCPP_2_, etc.
  dotnet run -- --env --multiple TEST_   # Loads TEST_1_, TEST_2_, etc.
```

**JSON Configuration:**
```
Usage: dotnet run -- --config <file.json> [--multiple]

Parameters:
  --config       - Load configuration from JSON file
  file.json      - Path to JSON configuration file
  --multiple     - Load array of configurations from JSON

Examples:
  dotnet run -- --config config.json           # Single configuration
  dotnet run -- --config configs.json --multiple  # Array of configurations
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

The simulator supports multiple ways to configure charging point settings:

### Environment Variables

**Single Configuration (OCPP_ prefix):**
```bash
OCPP_SERVER_URL=ws://localhost:5000/ocpp
OCPP_CHARGE_POINT_ID=CP001
OCPP_CHARGE_POINT_MODEL=ECO-DC-50
OCPP_CHARGE_POINT_VENDOR=Ecogy
OCPP_CHARGE_POINT_SERIAL=ECO001234567
OCPP_FIRMWARE_VERSION=1.0.0
OCPP_HEARTBEAT_INTERVAL=30
OCPP_TEST_RFID_CARD=abc123
OCPP_CONNECTOR_COUNT=1
OCPP_TENANT_ID=550e8400-e29b-41d4-a716-446655440000
```

**Multiple Configurations (numbered prefixes):**
```bash
# Configuration 1
OCPP_1_SERVER_URL=ws://localhost:5000/ocpp
OCPP_1_CHARGE_POINT_ID=CP001
OCPP_1_TEST_RFID_CARD=abc123

# Configuration 2
OCPP_2_SERVER_URL=ws://localhost:5000/ocpp
OCPP_2_CHARGE_POINT_ID=CP002
OCPP_2_TEST_RFID_CARD=def456

# Configuration 3
OCPP_3_SERVER_URL=ws://localhost:5000/ocpp
OCPP_3_CHARGE_POINT_ID=CP003
OCPP_3_TEST_RFID_CARD=ghi789
```

**Custom Prefix:**
```bash
# Use custom prefix like CHARGER_
CHARGER_SERVER_URL=ws://localhost:5000/ocpp
CHARGER_CHARGE_POINT_ID=CP001
CHARGER_TEST_RFID_CARD=abc123

# Run with custom prefix
dotnet run -- --env CHARGER_
```

### JSON Configuration

**Single Configuration:**
```json
{
  "serverUrl": "ws://localhost:5000/ocpp",
  "chargePointId": "CP001",
  "chargePointModel": "ECO-DC-50",
  "chargePointVendor": "Ecogy",
  "chargePointSerialNumber": "ECO001234567",
  "firmwareVersion": "1.0.0",
  "heartbeatInterval": 30,
  "testRfidCard": "abc123",
  "connectorCount": 1,
  "tenantId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Multiple Configurations:**
```json
[
  {
    "serverUrl": "ws://localhost:5000/ocpp",
    "chargePointId": "CP001",
    "testRfidCard": "abc123"
  },
  {
    "serverUrl": "ws://localhost:5000/ocpp",
    "chargePointId": "CP002",
    "testRfidCard": "def456"
  }
]
```

### Configuration Properties

| Property | Environment Variable | Default | Description |
|----------|---------------------|---------|-------------|
| ServerUrl | `{PREFIX}SERVER_URL` | `ws://localhost:5000/ocpp` | WebSocket URL of OCPP server |
| ChargePointId | `{PREFIX}CHARGE_POINT_ID` | `CP001` | Unique charging point identifier |
| ChargePointModel | `{PREFIX}CHARGE_POINT_MODEL` | `ECO-DC-50` | Charging point model name |
| ChargePointVendor | `{PREFIX}CHARGE_POINT_VENDOR` | `Ecogy` | Charging point vendor name |
| ChargePointSerialNumber | `{PREFIX}CHARGE_POINT_SERIAL` | `ECO001234567` | Serial number |
| FirmwareVersion | `{PREFIX}FIRMWARE_VERSION` | `1.0.0` | Firmware version |
| HeartbeatInterval | `{PREFIX}HEARTBEAT_INTERVAL` | `30` | Heartbeat interval in seconds |
| TestRfidCard | `{PREFIX}TEST_RFID_CARD` | `abc123` | Default RFID card for testing |
| ConnectorCount | `{PREFIX}CONNECTOR_COUNT` | `1` | Number of connectors |
| TenantId | `{PREFIX}TENANT_ID` | `null` | Tenant ID for multi-tenant scenarios |

**Note:** Replace `{PREFIX}` with your chosen prefix (default: `OCPP_`). For multiple configurations, use numbered prefixes like `OCPP_1_`, `OCPP_2_`, etc.

### Configuration Priority

The simulator loads configuration in the following priority order:

1. **Command Line Arguments** (highest priority)
2. **JSON Configuration File** 
3. **Environment Variables**
4. **Default Values** (lowest priority)

### Example Files

The repository includes example configuration files:

- `example-config.json` - Single configuration example
- `example-multiple-config.json` - Multiple configurations example
- `example-single.env` - Single configuration environment variables
- `example-multiple.env` - Multiple configurations environment variables

To use environment variable files on Linux/Mac:
```bash
source example-single.env
dotnet run -- --env
```

To use environment variable files on Windows:
```cmd
# Load variables manually or use a tool like dotenv
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

This project is licensed under the GPLv3 License - see the LICENSE file for details.

## Related Projects

- [OCPP 1.6 Specification](https://www.openchargealliance.org/protocols/ocpp-16/)
- [OCPP 2.0.1 Specification](https://www.openchargealliance.org/protocols/ocpp-201/)

## Support

For questions or issues, please open an issue on GitHub or contact the development team.

---

**Built with love for the EV charging ecosystem**

## Docker Deployment

The simulator supports Docker deployment for easy containerization and deployment in various environments.

### Building the Docker Image

```bash
# Build the Docker image
docker build -t ocpp-simulator .

# Or with a specific tag
docker build -t ocpp-simulator:latest .
```

### Running with Docker

### Single Charging Point (Environment Variables)

```bash
# Automated mode (default)
docker run -d --name ocpp-sim \
  -e OCPP_SERVER_URL="ws://your-server:5000/ocpp" \
  -e OCPP_CHARGE_POINT_ID="CP001" \
  -e OCPP_TEST_RFID_CARD="abc123" \
  ocpp-simulator

# Interactive mode
docker run -it --name ocpp-interactive \
  -e SIMULATOR_MODE="interactive" \
  -e OCPP_SERVER_URL="ws://your-server:5000/ocpp" \
  -e OCPP_CHARGE_POINT_ID="CP001" \
  -e OCPP_TEST_RFID_CARD="abc123" \
  ocpp-simulator
```

### Multiple Charging Points

```bash
docker run -d --name ocpp-multiple \
  -e SIMULATOR_MODE="automated" \
  -e CONFIG_SOURCE="env" \
  -e MULTIPLE_CONFIGS="true" \
  -e OCPP_1_SERVER_URL="ws://your-server:5000/ocpp" \
  -e OCPP_1_CHARGE_POINT_ID="CP001" \
  -e OCPP_1_TEST_RFID_CARD="abc123" \
  -e OCPP_2_SERVER_URL="ws://your-server:5000/ocpp" \
  -e OCPP_2_CHARGE_POINT_ID="CP002" \
  -e OCPP_2_TEST_RFID_CARD="def456" \
  ocpp-simulator
```

### JSON Configuration

```bash
# Create a custom config.json file, then:
docker run -d --name ocpp-json \
  -e SIMULATOR_MODE="automated" \
  -e CONFIG_SOURCE="json" \
  -e CONFIG_FILE="config.json" \
  -v $(pwd)/my-config.json:/app/config.json:ro \
  ocpp-simulator
```

### Docker Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `SIMULATOR_MODE` | `automated` | Mode to run: `automated` or `interactive` |
| `CONFIG_SOURCE` | `env` | Configuration source: `env`, `json`, or `args` |
| `ENV_PREFIX` | `OCPP_` | Prefix for environment variables |
| `MULTIPLE_CONFIGS` | `false` | Load multiple configurations |
| `CONFIG_FILE` | - | JSON configuration file path (when CONFIG_SOURCE=json) |
| `SERVER_URL` | - | Server URL (when CONFIG_SOURCE=args) |
| `CHARGE_POINT_ID` | - | Charge point ID (when CONFIG_SOURCE=args) |
| `TEST_RFID_CARD` | - | Test RFID card (when CONFIG_SOURCE=args) |

### Docker Compose

The repository includes a `docker-compose.yml` file with multiple deployment scenarios:

```bash
# Run single simulator
docker-compose up ocpp-simulator

# Run interactive simulator
docker-compose --profile interactive up ocpp-interactive

# Run multiple simulators
docker-compose --profile multiple up ocpp-multiple

# Run with JSON configuration
docker-compose --profile json up ocpp-json-config

# Run multiple configs from JSON
docker-compose --profile json-multiple up ocpp-json-multiple

# Run all automated simulators
docker-compose up
```

### Docker Compose Profiles

| Profile | Service | Description |
|---------|---------|-------------|
| (default) | `ocpp-simulator` | Single automated simulator |
| `interactive` | `ocpp-interactive` | Interactive mode simulator |
| `multiple` | `ocpp-multiple` | Multiple simulators from env vars |
| `json` | `ocpp-json-config` | Single config from JSON file |
| `json-multiple` | `ocpp-json-multiple` | Multiple configs from JSON file |

### Connecting to Local OCPP Server

When running the OCPP server on the host machine, use `host.docker.internal` instead of `localhost`:

```bash
# Instead of ws://localhost:5000/ocpp
# Use ws://host.docker.internal:5000/ocpp

docker run -d --name ocpp-sim \
  -e OCPP_SERVER_URL="ws://host.docker.internal:5000/ocpp" \
  -e OCPP_CHARGE_POINT_ID="CP001" \
  ocpp-simulator
```

### Production Deployment

For production deployments, consider:

1. **Resource Limits**:
```bash
docker run -d --name ocpp-sim \
  --memory="512m" \
  --cpus="0.5" \
  -e OCPP_SERVER_URL="ws://your-server:5000/ocpp" \
  ocpp-simulator
```

2. **Health Checks**:
```bash
# The image includes built-in health checks
docker run -d --name ocpp-sim \
  --health-cmd="ping -c 1 google.com" \
  --health-interval=30s \
  --health-timeout=10s \
  --health-retries=3 \
  ocpp-simulator
```

3. **Logging**:
```bash
# Configure logging driver
docker run -d --name ocpp-sim \
  --log-driver=json-file \
  --log-opt max-size=10m \
  --log-opt max-file=3 \
  ocpp-simulator
```

### Kubernetes Deployment

Example Kubernetes deployment:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ocpp-simulator
spec:
  replicas: 3
  selector:
    matchLabels:
      app: ocpp-simulator
  template:
    metadata:
      labels:
        app: ocpp-simulator
    spec:
      containers:
      - name: ocpp-simulator
        image: ocpp-simulator:latest
        env:
        - name: OCPP_SERVER_URL
          value: "ws://ocpp-server-service:5000/ocpp"
        - name: OCPP_CHARGE_POINT_ID
          valueFrom:
            fieldRef:
              fieldPath: metadata.name
        - name: OCPP_TEST_RFID_CARD
          value: "k8s-card"
        resources:
          requests:
            memory: "128Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
```

### Troubleshooting Docker

**Common Issues:**

1. **Connection Refused**: Ensure your OCPP server is accessible from the container
2. **DNS Resolution**: Use `host.docker.internal` for local servers
3. **Environment Variables**: Check variable names and prefixes match your configuration
4. **Interactive Mode**: Remember to use `-it` flags for interactive mode

**Debug Commands:**
```bash
# Check container logs
docker logs ocpp-sim

# Enter container for debugging
docker exec -it ocpp-sim /bin/bash

# Check environment variables
docker exec ocpp-sim env | grep OCPP

# Test network connectivity
docker exec ocpp-sim ping host.docker.internal