using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ecogy.app.chargepoint.simulator;
using ecogy.app.chargepoint.simulator.Interactive;

// Configure host and services for interactive mode
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
        // Load multiple configurations
        var prefix = args.Length > 2 ? args[2] : "OCPP_";
        configurations = ConfigurationLoader.LoadMultipleFromEnvironment(prefix);
        
        if (configurations.Count == 0)
        {
            Console.WriteLine($"No configurations found with prefix '{prefix}'. Use --help for more information.");
            ConfigurationLoader.PrintEnvironmentVariableHelp(prefix);
            Environment.Exit(1);
        }
        
        Console.WriteLine($"Found {configurations.Count} configuration(s) from environment variables.");
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
else if (args.Length >= 3)
{
    // Legacy command line arguments
    var config = ConfigurationLoader.LoadFromCommandLine(args);
    configurations.Add(config);
    Console.WriteLine("Using command line arguments for configuration.");
}
else
{
    Console.WriteLine("Error: Insufficient arguments provided.");
    ShowHelp();
    Environment.Exit(1);
}

// Ask user to select configuration if multiple are available
ChargingPointConfiguration selectedConfig;
if (configurations.Count > 1)
{
    Console.WriteLine("\nMultiple configurations available:");
    for (int i = 0; i < configurations.Count; i++)
    {
        var config = configurations[i];
        Console.WriteLine($"  {i + 1}: {config.ChargePointId} -> {config.ServerUrl}");
    }
    
    Console.Write($"\nSelect configuration (1-{configurations.Count}): ");
    var input = Console.ReadLine();
    
    if (!int.TryParse(input, out var selection) || selection < 1 || selection > configurations.Count)
    {
        Console.WriteLine("Invalid selection. Using first configuration.");
        selectedConfig = configurations[0];
    }
    else
    {
        selectedConfig = configurations[selection - 1];
    }
}
else
{
    selectedConfig = configurations[0];
}

Console.WriteLine($"\nSelected Configuration:");
Console.WriteLine($"  Server URL: {selectedConfig.ServerUrl}");
Console.WriteLine($"  Charge Point ID: {selectedConfig.ChargePointId}");
Console.WriteLine($"  Test RFID Card: {selectedConfig.TestRfidCard}");
Console.WriteLine($"  Model: {selectedConfig.ChargePointModel}");
Console.WriteLine($"  Vendor: {selectedConfig.ChargePointVendor}");

// Register services
builder.Services.AddSingleton(selectedConfig);
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

static void ShowHelp()
{
    Console.WriteLine("EV Charging Point OCPP Interactive Simulator");
    Console.WriteLine();
    Console.WriteLine("Usage Options:");
    Console.WriteLine("  1. Command Line Arguments:");
    Console.WriteLine("     dotnet run -- <serverUrl> <chargePointId> <testRfidCard>");
    Console.WriteLine("     Example: dotnet run -- \"ws://localhost:5000/ocpp\" \"CP001\" \"abc123\"");
    Console.WriteLine();
    Console.WriteLine("  2. Environment Variables:");
    Console.WriteLine("     dotnet run -- --env [prefix]");
    Console.WriteLine("     dotnet run -- --env --multiple [prefix]  (for multiple configs)");
    Console.WriteLine("     Example: dotnet run -- --env OCPP_");
    Console.WriteLine();
    Console.WriteLine("  3. JSON Configuration File:");
    Console.WriteLine("     dotnet run -- --config <file.json>");
    Console.WriteLine("     dotnet run -- --config <file.json> --multiple  (for array of configs)");
    Console.WriteLine("     Example: dotnet run -- --config config.json");
    Console.WriteLine();
    Console.WriteLine("Environment Variable Examples:");
    Console.WriteLine("  Single configuration:");
    Console.WriteLine("    OCPP_SERVER_URL=ws://localhost:5000/ocpp");
    Console.WriteLine("    OCPP_CHARGE_POINT_ID=CP001");
    Console.WriteLine("    OCPP_TEST_RFID_CARD=abc123");
    Console.WriteLine();
    Console.WriteLine("  Multiple configurations:");
    Console.WriteLine("    OCPP_1_SERVER_URL=ws://localhost:5000/ocpp");
    Console.WriteLine("    OCPP_1_CHARGE_POINT_ID=CP001");
    Console.WriteLine("    OCPP_1_TEST_RFID_CARD=abc123");
    Console.WriteLine("    OCPP_2_SERVER_URL=ws://localhost:5000/ocpp");
    Console.WriteLine("    OCPP_2_CHARGE_POINT_ID=CP002");
    Console.WriteLine("    OCPP_2_TEST_RFID_CARD=def456");
    Console.WriteLine();
    ConfigurationLoader.PrintEnvironmentVariableHelp();
}