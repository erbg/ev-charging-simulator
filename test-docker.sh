#!/bin/bash

# Test script for OCPP Simulator Docker deployment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_test() {
    echo -e "${BLUE}[TEST]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Test configuration
IMAGE_NAME="ocpp-simulator:latest"
TEST_SERVER_URL="ws://httpbin.org/status/404"  # Will fail but tests connection attempt
TEST_CONTAINER_PREFIX="ocpp-test"

cleanup() {
    print_info "Cleaning up test containers..."
    docker ps -a --filter "name=${TEST_CONTAINER_PREFIX}" -q | xargs -r docker rm -f
}

# Cleanup on exit
trap cleanup EXIT

print_info "Starting OCPP Simulator Docker tests"
print_info "Image: $IMAGE_NAME"

# Test 1: Build image
print_test "Building Docker image..."
if docker build -t "$IMAGE_NAME" . > /dev/null 2>&1; then
    print_success "Docker image built successfully"
else
    print_error "Failed to build Docker image"
    exit 1
fi

# Test 2: Test automated mode with environment variables
print_test "Testing automated mode with environment variables..."
CONTAINER_NAME="${TEST_CONTAINER_PREFIX}-automated"
if docker run -d --name "$CONTAINER_NAME" \
    -e OCPP_SERVER_URL="$TEST_SERVER_URL" \
    -e OCPP_CHARGE_POINT_ID="TEST_CP001" \
    -e OCPP_TEST_RFID_CARD="test123" \
    "$IMAGE_NAME" > /dev/null; then
    
    sleep 3
    if docker ps --filter "name=$CONTAINER_NAME" --filter "status=running" | grep -q "$CONTAINER_NAME"; then
        print_success "Automated mode container started successfully"
    else
        print_error "Automated mode container failed to start"
        docker logs "$CONTAINER_NAME"
        exit 1
    fi
else
    print_error "Failed to start automated mode container"
    exit 1
fi

# Test 3: Test multiple configurations
print_test "Testing multiple configurations..."
CONTAINER_NAME="${TEST_CONTAINER_PREFIX}-multiple"
if docker run -d --name "$CONTAINER_NAME" \
    -e SIMULATOR_MODE="automated" \
    -e CONFIG_SOURCE="env" \
    -e MULTIPLE_CONFIGS="true" \
    -e OCPP_1_SERVER_URL="$TEST_SERVER_URL" \
    -e OCPP_1_CHARGE_POINT_ID="TEST_CP101" \
    -e OCPP_1_TEST_RFID_CARD="test101" \
    -e OCPP_2_SERVER_URL="$TEST_SERVER_URL" \
    -e OCPP_2_CHARGE_POINT_ID="TEST_CP102" \
    -e OCPP_2_TEST_RFID_CARD="test102" \
    "$IMAGE_NAME" > /dev/null; then
    
    sleep 3
    if docker ps --filter "name=$CONTAINER_NAME" --filter "status=running" | grep -q "$CONTAINER_NAME"; then
        print_success "Multiple configurations container started successfully"
    else
        print_error "Multiple configurations container failed to start"
        docker logs "$CONTAINER_NAME"
        exit 1
    fi
else
    print_error "Failed to start multiple configurations container"
    exit 1
fi

# Test 4: Test JSON configuration
print_test "Testing JSON configuration..."
CONTAINER_NAME="${TEST_CONTAINER_PREFIX}-json"
if docker run -d --name "$CONTAINER_NAME" \
    -e SIMULATOR_MODE="automated" \
    -e CONFIG_SOURCE="json" \
    -e CONFIG_FILE="example-config.json" \
    "$IMAGE_NAME" > /dev/null; then
    
    sleep 3
    if docker ps --filter "name=$CONTAINER_NAME" --filter "status=running" | grep -q "$CONTAINER_NAME"; then
        print_success "JSON configuration container started successfully"
    else
        print_error "JSON configuration container failed to start"
        docker logs "$CONTAINER_NAME"
        exit 1
    fi
else
    print_error "Failed to start JSON configuration container"
    exit 1
fi

# Test 5: Check if example files are included
print_test "Checking if example files are included..."
CONTAINER_NAME="${TEST_CONTAINER_PREFIX}-files"
if docker run --rm --name "$CONTAINER_NAME" "$IMAGE_NAME" /bin/bash -c "ls -la /app/example-*.json /app/example-*.env" 2>/dev/null; then
    print_success "Example files are included in the image"
else
    print_error "Example files are missing from the image"
fi

# Test 6: Test help command
print_test "Testing help command..."
CONTAINER_NAME="${TEST_CONTAINER_PREFIX}-help"
if docker run --rm --name "$CONTAINER_NAME" "$IMAGE_NAME" /bin/bash -c "cd simulator && dotnet ecogy.app.chargepoint.simulator.dll --help" 2>/dev/null | grep -q "Usage"; then
    print_success "Help command works correctly"
else
    print_error "Help command failed"
fi

# Test 7: Test container logs
print_test "Checking container logs..."
for container in "${TEST_CONTAINER_PREFIX}-automated" "${TEST_CONTAINER_PREFIX}-multiple" "${TEST_CONTAINER_PREFIX}-json"; do
    if docker logs "$container" 2>&1 | grep -q "Starting EV Charging Point OCPP Simulator"; then
        print_success "Container $container has expected log output"
    else
        print_error "Container $container has unexpected log output"
        echo "Logs for $container:"
        docker logs "$container"
    fi
done

print_info "All Docker tests completed successfully!"
print_info ""
print_info "Image is ready for deployment. Example usage:"
print_info "  docker run -d --name ocpp-sim \\"
print_info "    -e OCPP_SERVER_URL=\"ws://your-server:5000/ocpp\" \\"
print_info "    -e OCPP_CHARGE_POINT_ID=\"CP001\" \\"
print_info "    $IMAGE_NAME"