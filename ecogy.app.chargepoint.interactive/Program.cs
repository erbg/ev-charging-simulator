using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ecogy.app.chargepoint.simulator;
using ecogy.app.chargepoint.simulator.Interactive;

// Configure host and services for interactive mode
var builder = Host.CreateApplicationBuilder(args);

// Add logging
builder.Services.AddLogging(configure => configure.AddConsole());

if (args.Length<3 || args[0] == "--help" || args[0] == "-h")
{
    Console.WriteLine("Usage: dotnet run <serverUrl> <chargePointId> <testRfidCard>");
    Console.WriteLine("Example: dotnet run ws://localhost:5000/ocpp/websocket/CentralSystemService CP001 abc123");
    Console.WriteLine("Using default values for missing arguments.");
    Environment.Exit(0);
}

// Configure charging point
var config = new ChargingPointConfiguration
{
    ServerUrl = args[0],
    ChargePointId = args[1],
    TestRfidCard = args[2]
};

// Register services
builder.Services.AddSingleton(config);
builder.Services.AddSingleton<ChargingPointSimulator>();
builder.Services.AddTransient<InteractiveSimulator>();

// Build host
var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    // Start the charging point simulator in the background
    var simulator = host.Services.GetRequiredService<ChargingPointSimulator>();
    var cancellationTokenSource = new CancellationTokenSource();
    
    logger.LogInformation("Starting charging point background service...");
    var simulatorTask = simulator.StartAsync(cancellationTokenSource.Token);

    // Wait a moment for connection
    await Task.Delay(2000);

    // Start interactive console
    var interactiveSimulator = host.Services.GetRequiredService<InteractiveSimulator>();
    await interactiveSimulator.RunAsync(cancellationTokenSource.Token);

    // Cleanup
    cancellationTokenSource.Cancel();
    await simulator.StopAsync(CancellationToken.None);
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred while running the interactive charging point simulator");
}

logger.LogInformation("Interactive charging point simulation stopped.");