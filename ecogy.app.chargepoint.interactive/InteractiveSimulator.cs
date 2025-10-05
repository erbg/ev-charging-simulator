using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ecogy.app.chargepoint.simulator;

namespace ecogy.app.chargepoint.simulator.Interactive;

/// <summary>
/// Interactive console application for manually controlling the charging point simulator
/// </summary>
public class InteractiveSimulator
{
    private readonly ILogger<InteractiveSimulator> _logger;
    private readonly ChargingPointSimulator _simulator;
    private readonly ChargingPointConfiguration _config;
    private bool _isRunning = true;

    public InteractiveSimulator(ILogger<InteractiveSimulator> logger, ChargingPointSimulator simulator, ChargingPointConfiguration config)
    {
        _logger = logger;
        _simulator = simulator;
        _config = config;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== Interactive OCPP Charging Point Simulator ===");
        _logger.LogInformation("Charging Point ID: {ChargePointId}", _config.ChargePointId);
        _logger.LogInformation("Available Commands:");
        _logger.LogInformation("  1 - Authorize RFID card");
        _logger.LogInformation("  2 - Simulate complete transaction");
        _logger.LogInformation("  3 - Change connector status");
        _logger.LogInformation("  4 - Show current configuration");
        _logger.LogInformation("  h - Show help");
        _logger.LogInformation("  q - Quit");
        _logger.LogInformation("=================================================");

        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                Console.Write("\nEnter command: ");
                var input = Console.ReadLine()?.Trim().ToLower();

                if (string.IsNullOrEmpty(input))
                    continue;

                await ProcessCommand(input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing command");
            }
        }
    }

    private async Task ProcessCommand(string command)
    {
        switch (command)
        {
            case "1":
                await HandleAuthorizeCommand();
                break;
            case "2":
                await HandleTransactionCommand();
                break;
            case "3":
                await HandleStatusChangeCommand();
                break;
            case "4":
                ShowConfiguration();
                break;
            case "h":
            case "help":
                ShowHelp();
                break;
            case "q":
            case "quit":
            case "exit":
                _isRunning = false;
                break;
            default:
                Console.WriteLine("Unknown command. Type 'h' for help.");
                break;
        }
    }

    private async Task HandleAuthorizeCommand()
    {
        Console.Write("Enter RFID card ID (or press Enter for default '{0}'): ", _config.TestRfidCard);
        var idTag = Console.ReadLine()?.Trim();
        
        if (string.IsNullOrEmpty(idTag))
            idTag = _config.TestRfidCard;

        await _simulator.SimulateAuthorization(idTag);
        Console.WriteLine($"Authorization request sent for card: {idTag}");
    }

    private async Task HandleTransactionCommand()
    {
        Console.Write("Enter RFID card ID (or press Enter for default '{0}'): ", _config.TestRfidCard);
        var idTag = Console.ReadLine()?.Trim();
        
        if (string.IsNullOrEmpty(idTag))
            idTag = _config.TestRfidCard;

        Console.WriteLine($"Starting complete transaction simulation for card: {idTag}");
        Console.WriteLine("This will:");
        Console.WriteLine("  1. Send Authorize request");
        Console.WriteLine("  2. Start transaction");
        Console.WriteLine("  3. Send meter values");
        Console.WriteLine("  4. Stop transaction");
        Console.WriteLine("Please wait...");

        await _simulator.SimulateTransaction(idTag);
        Console.WriteLine("Transaction simulation completed!");
    }

    private async Task HandleStatusChangeCommand()
    {
        Console.WriteLine("Available connector statuses:");
        Console.WriteLine("  1 - Available");
        Console.WriteLine("  2 - Preparing");
        Console.WriteLine("  3 - Charging");
        Console.WriteLine("  4 - SuspendedEVSE");
        Console.WriteLine("  5 - SuspendedEV");
        Console.WriteLine("  6 - Finishing");
        Console.WriteLine("  7 - Reserved");
        Console.WriteLine("  8 - Unavailable");
        Console.WriteLine("  9 - Faulted");

        Console.Write("Select status (1-9): ");
        var statusChoice = Console.ReadLine()?.Trim();

        var status = statusChoice switch
        {
            "1" => "Available",
            "2" => "Preparing",
            "3" => "Charging",
            "4" => "SuspendedEVSE",
            "5" => "SuspendedEV",
            "6" => "Finishing",
            "7" => "Reserved",
            "8" => "Unavailable",
            "9" => "Faulted",
            _ => null
        };

        if (status != null)
        {
            await _simulator.SimulateConnectorStatusChange(status);
            Console.WriteLine($"Status notification sent: {status}");
        }
        else
        {
            Console.WriteLine("Invalid status choice.");
        }
    }

    private void ShowConfiguration()
    {
        Console.WriteLine("\n=== Current Configuration ===");
        Console.WriteLine($"Server URL: {_config.ServerUrl}");
        Console.WriteLine($"Charging Point ID: {_config.ChargePointId}");
        Console.WriteLine($"Model: {_config.ChargePointModel}");
        Console.WriteLine($"Vendor: {_config.ChargePointVendor}");
        Console.WriteLine($"Serial Number: {_config.ChargePointSerialNumber}");
        Console.WriteLine($"Firmware Version: {_config.FirmwareVersion}");
        Console.WriteLine($"Heartbeat Interval: {_config.HeartbeatInterval}s");
        Console.WriteLine($"Test RFID Card: {_config.TestRfidCard}");
        Console.WriteLine($"Current Transaction ID: {_config.CurrentTransactionId?.ToString() ?? "None"}");
        Console.WriteLine($"Current Meter Value: {_config.CurrentMeterValue} Wh");
        Console.WriteLine("============================");
    }

    private void ShowHelp()
    {
        Console.WriteLine("\n=== Available Commands ===");
        Console.WriteLine("1 - Authorize RFID card");
        Console.WriteLine("    Sends an Authorize request to test RFID card validation");
        Console.WriteLine();
        Console.WriteLine("2 - Simulate complete transaction");
        Console.WriteLine("    Runs a full charging session:");
        Console.WriteLine("    - Authorize ? Start Transaction ? Meter Values ? Stop Transaction");
        Console.WriteLine();
        Console.WriteLine("3 - Change connector status");
        Console.WriteLine("    Sends StatusNotification with selected connector status");
        Console.WriteLine();
        Console.WriteLine("4 - Show current configuration");
        Console.WriteLine("    Displays all current simulator settings");
        Console.WriteLine();
        Console.WriteLine("h - Show this help");
        Console.WriteLine("q - Quit the simulator");
        Console.WriteLine("==========================");
    }
}