using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ecogy.app.chargepoint.simulator;

// Configure host and services
var builder = Host.CreateApplicationBuilder(args);

// Add logging
builder.Services.AddLogging(configure => configure.AddConsole());

// Configure charging point
var config = new ChargingPointConfiguration
{
    ServerUrl = args.Length > 0 ? args[0] : "ws://localhost:5000/ocpp",
    ChargePointId = args.Length > 1 ? args[1] : "CP001",
    TestRfidCard = args.Length > 2 ? args[2] : "abc123"
};

// Register services
builder.Services.AddSingleton(config);
builder.Services.AddHostedService<ChargingPointSimulator>();

// Build and run
var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== OCPP Charging Point Simulator ===");
logger.LogInformation("Server URL: {ServerUrl}", config.ServerUrl);
logger.LogInformation("Charging Point ID: {ChargePointId}", config.ChargePointId);
logger.LogInformation("Test RFID Card: {TestRfidCard}", config.TestRfidCard);
logger.LogInformation("========================================");

logger.LogInformation("Starting charging point simulation...");
logger.LogInformation("Commands you can test:");
logger.LogInformation("  - The simulator will automatically send BootNotification, Heartbeats, and StatusNotifications");
logger.LogInformation("  - Use your API to send RemoteStartTransaction/RemoteStopTransaction commands");
logger.LogInformation("  - The simulator will respond appropriately and send StartTransaction/StopTransaction messages");
logger.LogInformation("  - Press Ctrl+C to stop the simulation");

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred while running the charging point simulator");
}

logger.LogInformation("Charging point simulation stopped.");