using System.Text.Json;

namespace ecogy.app.chargepoint.simulator;

/// <summary>
/// Utility class for loading charging point configurations from various sources
/// </summary>
public static class ConfigurationLoader
{
    /// <summary>
    /// Load configuration from environment variables
    /// </summary>
    /// <param name="prefix">Environment variable prefix (e.g., "OCPP_" for OCPP_SERVER_URL)</param>
    /// <returns>Configuration object populated from environment variables</returns>
    public static ChargingPointConfiguration LoadFromEnvironment(string prefix = "OCPP_")
    {
        var config = new ChargingPointConfiguration();

        // Load each property from environment variables
        if (GetEnvironmentVariable($"{prefix}SERVER_URL") is string serverUrl)
            config.ServerUrl = serverUrl;

        if (GetEnvironmentVariable($"{prefix}CHARGE_POINT_ID") is string chargePointId)
            config.ChargePointId = chargePointId;

        if (GetEnvironmentVariable($"{prefix}CHARGE_POINT_MODEL") is string model)
            config.ChargePointModel = model;

        if (GetEnvironmentVariable($"{prefix}CHARGE_POINT_VENDOR") is string vendor)
            config.ChargePointVendor = vendor;

        if (GetEnvironmentVariable($"{prefix}CHARGE_POINT_SERIAL") is string serial)
            config.ChargePointSerialNumber = serial;

        if (GetEnvironmentVariable($"{prefix}FIRMWARE_VERSION") is string firmware)
            config.FirmwareVersion = firmware;

        if (GetEnvironmentVariable($"{prefix}HEARTBEAT_INTERVAL") is string heartbeatStr &&
            int.TryParse(heartbeatStr, out var heartbeat))
            config.HeartbeatInterval = heartbeat;

        if (GetEnvironmentVariable($"{prefix}TEST_RFID_CARD") is string rfidCard)
            config.TestRfidCard = rfidCard;

        if (GetEnvironmentVariable($"{prefix}CONNECTOR_COUNT") is string connectorStr &&
            int.TryParse(connectorStr, out var connectorCount))
            config.ConnectorCount = connectorCount;

        if (GetEnvironmentVariable($"{prefix}TENANT_ID") is string tenantStr &&
            Guid.TryParse(tenantStr, out var tenantId))
            config.TenantId = tenantId;

        return config;
    }

    /// <summary>
    /// Load multiple configurations from environment variables
    /// Supports patterns like OCPP_1_SERVER_URL, OCPP_2_SERVER_URL, etc.
    /// </summary>
    /// <param name="prefix">Base environment variable prefix</param>
    /// <param name="maxConfigurations">Maximum number of configurations to search for</param>
    /// <returns>List of configuration objects</returns>
    public static List<ChargingPointConfiguration> LoadMultipleFromEnvironment(string prefix = "OCPP_", int maxConfigurations = 10)
    {
        var configurations = new List<ChargingPointConfiguration>();

        for (int i = 1; i <= maxConfigurations; i++)
        {
            var instancePrefix = $"{prefix}{i}_";
            
            // Check if this configuration instance exists by looking for a required field
            if (GetEnvironmentVariable($"{instancePrefix}SERVER_URL") != null ||
                GetEnvironmentVariable($"{instancePrefix}CHARGE_POINT_ID") != null)
            {
                var config = LoadFromEnvironment(instancePrefix);
                configurations.Add(config);
            }
        }

        // If no numbered configurations found, try to load a single configuration
        if (configurations.Count == 0)
        {
            var singleConfig = LoadFromEnvironment(prefix);
            // Only add if it has meaningful values (not all defaults)
            if (!string.IsNullOrEmpty(GetEnvironmentVariable($"{prefix}SERVER_URL")) ||
                !string.IsNullOrEmpty(GetEnvironmentVariable($"{prefix}CHARGE_POINT_ID")))
            {
                configurations.Add(singleConfig);
            }
        }

        return configurations;
    }

    /// <summary>
    /// Load configuration from JSON file
    /// </summary>
    /// <param name="filePath">Path to JSON configuration file</param>
    /// <returns>Configuration object from JSON file</returns>
    public static ChargingPointConfiguration LoadFromJsonFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<ChargingPointConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return config ?? new ChargingPointConfiguration();
    }

    /// <summary>
    /// Load multiple configurations from JSON file
    /// </summary>
    /// <param name="filePath">Path to JSON configuration file containing an array of configurations</param>
    /// <returns>List of configuration objects</returns>
    public static List<ChargingPointConfiguration> LoadMultipleFromJsonFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var configurations = JsonSerializer.Deserialize<List<ChargingPointConfiguration>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return configurations ?? new List<ChargingPointConfiguration>();
    }

    /// <summary>
    /// Create configuration from command line arguments
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Configuration object from command line arguments</returns>
    public static ChargingPointConfiguration LoadFromCommandLine(string[] args)
    {
        var config = new ChargingPointConfiguration();

        if (args.Length >= 1) config.ServerUrl = args[0];
        if (args.Length >= 2) config.ChargePointId = args[1];
        if (args.Length >= 3) config.TestRfidCard = args[2];

        return config;
    }

    /// <summary>
    /// Print available environment variables for configuration
    /// </summary>
    /// <param name="prefix">Environment variable prefix</param>
    public static void PrintEnvironmentVariableHelp(string prefix = "OCPP_")
    {
        Console.WriteLine($"Environment Variables for Configuration (prefix: {prefix}):");
        Console.WriteLine($"  {prefix}SERVER_URL           - WebSocket URL of the OCPP server");
        Console.WriteLine($"  {prefix}CHARGE_POINT_ID      - Unique identifier for the charging point");
        Console.WriteLine($"  {prefix}CHARGE_POINT_MODEL   - Model name of the charging point");
        Console.WriteLine($"  {prefix}CHARGE_POINT_VENDOR  - Vendor name of the charging point");
        Console.WriteLine($"  {prefix}CHARGE_POINT_SERIAL  - Serial number of the charging point");
        Console.WriteLine($"  {prefix}FIRMWARE_VERSION     - Firmware version of the charging point");
        Console.WriteLine($"  {prefix}HEARTBEAT_INTERVAL   - Heartbeat interval in seconds");
        Console.WriteLine($"  {prefix}TEST_RFID_CARD       - Test RFID card for transactions");
        Console.WriteLine($"  {prefix}CONNECTOR_COUNT      - Number of connectors");
        Console.WriteLine($"  {prefix}TENANT_ID            - Tenant ID for multi-tenant scenarios");
        Console.WriteLine();
        Console.WriteLine("For multiple configurations, use numbered prefixes:");
        Console.WriteLine($"  {prefix}1_SERVER_URL, {prefix}1_CHARGE_POINT_ID, etc.");
        Console.WriteLine($"  {prefix}2_SERVER_URL, {prefix}2_CHARGE_POINT_ID, etc.");
    }

    private static string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}