# Docker Deployment Guide

This guide covers deploying the EV Charging Point OCPP Simulator using Docker.

## Quick Start

### 1. Build the Image
```bash
# Simple build
docker build -t ocpp-simulator .

```

### 2. Run Single Simulator
```bash
docker run -d --name ocpp-sim \
  -e OCPP_SERVER_URL="ws://your-server:5000/ocpp" \
  -e OCPP_CHARGE_POINT_ID="CP001" \
  -e OCPP_TEST_RFID_CARD="abc123" \
  ocpp-simulator
```

### 3. Use Docker Compose
```bash
# Edit docker-compose.yml with your server details
docker-compose up
```

## Configuration Options

### Environment Variables Method
- Set `CONFIG_SOURCE=env` (default)
- Use `OCPP_*` environment variables
- For multiple configs, set `MULTIPLE_CONFIGS=true` and use `OCPP_1_*`, `OCPP_2_*`, etc.

### JSON Configuration Method
- Set `CONFIG_SOURCE=json`
- Set `CONFIG_FILE=your-config.json`
- Mount your JSON file as volume: `-v $(pwd)/config.json:/app/config.json:ro`

### Command Line Arguments Method
- Set `CONFIG_SOURCE=args`
- Set `SERVER_URL`, `CHARGE_POINT_ID`, `TEST_RFID_CARD` environment variables

## Deployment Scenarios

### Single Charging Point
```bash
docker run -d --name ocpp-cp001 \
  -e OCPP_SERVER_URL="ws://ocpp-server:5000/ocpp" \
  -e OCPP_CHARGE_POINT_ID="CP001" \
  -e OCPP_TEST_RFID_CARD="card001" \
  ocpp-simulator
```

### Multiple Charging Points (Same Container)
```bash
docker run -d --name ocpp-multiple \
  -e MULTIPLE_CONFIGS=true \
  -e OCPP_1_CHARGE_POINT_ID="CP001" \
  -e OCPP_1_TEST_RFID_CARD="card001" \
  -e OCPP_2_CHARGE_POINT_ID="CP002" \
  -e OCPP_2_TEST_RFID_CARD="card002" \
  ocpp-simulator
```

### Load Testing (Multiple Containers)
```bash
# Start multiple containers
for i in {1..10}; do
  docker run -d --name "ocpp-cp$(printf "%03d" $i)" \
    -e OCPP_CHARGE_POINT_ID="CP$(printf "%03d" $i)" \
    -e OCPP_TEST_RFID_CARD="card$(printf "%03d" $i)" \
    ocpp-simulator
done
```

### Interactive Mode
```bash
docker run -it --name ocpp-interactive \
  -e SIMULATOR_MODE=interactive \
  -e OCPP_SERVER_URL="ws://ocpp-server:5000/ocpp" \
  -e OCPP_CHARGE_POINT_ID="CP001" \
  ocpp-simulator
```

## Monitoring and Debugging

### View Logs
```bash
# Follow logs
docker logs -f ocpp-sim

# View specific number of lines
docker logs --tail 100 ocpp-sim
```

### Enter Container
```bash
docker exec -it ocpp-sim /bin/bash
```

### Check Environment
```bash
docker exec ocpp-sim env | grep OCPP
```

### Health Check
```bash
docker inspect --format='{{.State.Health.Status}}' ocpp-sim
```

## Production Considerations

### Resource Limits
```bash
docker run -d --name ocpp-sim \
  --memory="256m" \
  --cpus="0.25" \
  -e OCPP_SERVER_URL="ws://ocpp-server:5000/ocpp" \
  ocpp-simulator
```

### Restart Policies
```bash
docker run -d --name ocpp-sim \
  --restart=unless-stopped \
  -e OCPP_SERVER_URL="ws://ocpp-server:5000/ocpp" \
  ocpp-simulator
```

### Logging Configuration
```bash
docker run -d --name ocpp-sim \
  --log-driver=json-file \
  --log-opt max-size=10m \
  --log-opt max-file=5 \
  ocpp-simulator
```

## Troubleshooting

### Common Issues

1. **Cannot connect to server**
   - Check if server URL is accessible from container
   - Use `host.docker.internal` for local servers
   - Verify network configuration

2. **Container exits immediately**
   - Check logs: `docker logs container-name`
   - Verify environment variables
   - Check configuration format

3. **Environment variables not working**
   - Verify variable names and prefixes
   - Check for typos in variable names
   - Ensure proper escaping of special characters

### Debug Commands
```bash
# Test network connectivity
docker exec ocpp-sim ping ocpp-server

# Check DNS resolution
docker exec ocpp-sim nslookup ocpp-server

# View environment variables
docker exec ocpp-sim printenv | grep OCPP

# Test configuration loading
docker exec ocpp-sim /bin/bash -c "cd simulator && dotnet ecogy.app.chargepoint.simulator.dll --help"
```

## Files Included

- `Dockerfile` - Multi-stage build for optimized image
- `docker-compose.yml` - Multiple deployment scenarios
- `.dockerignore` - Optimized build context
- `build-docker.sh` - Build script with options
- `test-docker.sh` - Automated testing script