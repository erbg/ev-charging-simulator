using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ecogy.app.chargepoint.simulator;

// Configure host and services
var builder = Host.CreateApplicationBuilder(args);

// Add logging
builder.Services.AddLogging(configure => configure.AddConsole());

// Parse command line arguments and determine configuration source
var configurations = new List<ChargingPointConfiguration>();

if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
{
    ShowHelp();
    Environment.Exit(0);
}

if (args.Length > 0 && (args[0] == "--env" || args[0] == "-e"))
{
    // Load from environment variables
    Console.WriteLine("Loading configuration from environment variables...");
    
    if (args.Length > 1 && (args[1] == "--multiple" || args[1] == "-m"))
    {
        // Load multiple configurations - for automated mode, we'll run them all simultaneously
        var prefix = args.Length > 2 ? args[2] : "OCPP_";
        configurations = ConfigurationLoader.LoadMultipleFromEnvironment(prefix);
        
        if (configurations.Count == 0)
        {
            Console.WriteLine($"No configurations found with prefix '{prefix}'. Use --help for more information.");
            ConfigurationLoader.PrintEnvironmentVariableHelp(prefix);
            Environment.Exit(1);
        }
        
        Console.WriteLine($"Found {configurations.Count} configuration(s) from environment variables.");
        Console.WriteLine("Will start all configurations simultaneously.");
    }
    else
    {
        // Load single configuration
        var prefix = args.Length > 1 ? args[1] : "OCPP_";
        var config = ConfigurationLoader.LoadFromEnvironment(prefix);
        configurations.Add(config);
        Console.WriteLine("Loaded single configuration from environment variables.");
    }
}
else if (args.Length > 0 && (args[0] == "--config" || args[0] == "-c"))
{
    // Load from JSON file
    if (args.Length < 2)
    {
        Console.WriteLine("Error: --config requires a file path argument.");
        Environment.Exit(1);
    }
    
    var filePath = args[1];
    Console.WriteLine($"Loading configuration from file: {filePath}");
    
    try
    {
        if (args.Length > 2 && (args[2] == "--multiple" || args[2] == "-m"))
        {
            configurations = ConfigurationLoader.LoadMultipleFromJsonFile(filePath);
            Console.WriteLine("Will start all configurations simultaneously.");
        }
        else
        {
            var config = ConfigurationLoader.LoadFromJsonFile(filePath);
            configurations.Add(config);
        }
        
        Console.WriteLine($"Loaded {configurations.Count} configuration(s) from file.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading configuration file: {ex.Message}");
        Environment.Exit(1);
    }
}
else if (args.Length >= 1)
{
    // Legacy command line arguments or single argument scenarios
    var config = ConfigurationLoader.LoadFromCommandLine(args);
    configurations.Add(config);
    Console.WriteLine("Using command line arguments for configuration.");
}
else
{
    // No arguments - use defaults
    var config = new ChargingPointConfiguration();
    configurations.Add(config);
    Console.WriteLine("Using default configuration.");
}

// Register all configurations and simulators
if (configurations.Count == 1)
{
    // Single configuration - use original approach
    var config = configurations[0];
    builder.Services.AddSingleton(config);
    builder.Services.AddHostedService<ChargingPointSimulator>();
}
else
{
    // Multiple configurations - register multiple simulators
    for (int i = 0; i < configurations.Count; i++)
    {
        var config = configurations[i];
        builder.Services.AddKeyedSingleton($"config_{i}", config);
        builder.Services.AddKeyedSingleton<ChargingPointSimulator>($"simulator_{i}", (provider, key) =>
        {
            var logger = provider.GetRequiredService<ILogger<ChargingPointSimulator>>();
            var configuration = provider.GetRequiredKeyedService<ChargingPointConfiguration>($"config_{i}");
            return new ChargingPointSimulator(logger, configuration);
        });
        builder.Services.AddHostedService(provider => 
            provider.GetRequiredKeyedService<ChargingPointSimulator>($"simulator_{i}"));
    }
}

// Build and run
var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== OCPP Charging Point Simulator ===");

if (configurations.Count == 1)
{
    var config = configurations[0];
    logger.LogInformation("Server URL: {ServerUrl}", config.ServerUrl);
    logger.LogInformation("Charging Point ID: {ChargePointId}", config.ChargePointId);
    logger.LogInformation("Test RFID Card: {TestRfidCard}", config.TestRfidCard);
}
else
{
    logger.LogInformation("Running {Count} charging point simulators:", configurations.Count);
    for (int i = 0; i < configurations.Count; i++)
    {
        var config = configurations[i];
        logger.LogInformation("  [{Index}] {ChargePointId} -> {ServerUrl} (RFID: {TestRfidCard})", 
            i + 1, config.ChargePointId, config.ServerUrl, config.TestRfidCard);
    }
}

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

static void ShowHelp()
{
    Console.WriteLine("EV Charging Point OCPP Automated Simulator");
    Console.WriteLine();
    Console.WriteLine("Usage Options:");
    Console.WriteLine("  1. Command Line Arguments:");
    Console.WriteLine("     dotnet run -- <serverUrl> <chargePointId> <testRfidCard>");
    Console.WriteLine("     Example: dotnet run -- \"ws://localhost:5000/ocpp\" \"CP001\" \"abc123\"");
    Console.WriteLine();
    Console.WriteLine("  2. Environment Variables:");
    Console.WriteLine("     dotnet run -- --env [prefix]");
    Console.WriteLine("     dotnet run -- --env --multiple [prefix]  (runs all configs simultaneously)");
    Console.WriteLine("     Example: dotnet run -- --env OCPP_");
    Console.WriteLine();
    Console.WriteLine("  3. JSON Configuration File:");
    Console.WriteLine("     dotnet run -- --config <file.json>");
    Console.WriteLine("     dotnet run -- --config <file.json> --multiple  (runs array of configs)");
    Console.WriteLine("     Example: dotnet run -- --config config.json");
    Console.WriteLine();
    Console.WriteLine("  4. Default Configuration:");
    Console.WriteLine("     dotnet run");
    Console.WriteLine("     Uses built-in defaults (ws://localhost:5000/ocpp, CP001, abc123)");
    Console.WriteLine();
    ConfigurationLoader.PrintEnvironmentVariableHelp();
}