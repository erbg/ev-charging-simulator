namespace ecogy.app.chargepoint.simulator;

/// <summary>
/// Configuration for the charging point simulator
/// </summary>
public class ChargingPointConfiguration
{
    /// <summary>
    /// The WebSocket URL of the OCPP server
    /// </summary>
    public string ServerUrl { get; set; } = "ws://localhost:5000/ocpp";

    /// <summary>
    /// Unique identifier for this charging point
    /// </summary>
    public string ChargePointId { get; set; } = "CP001";

    /// <summary>
    /// Model name of the charging point
    /// </summary>
    public string ChargePointModel { get; set; } = "ECO-DC-50";

    /// <summary>
    /// Vendor name of the charging point
    /// </summary>
    public string ChargePointVendor { get; set; } = "Ecogy";

    /// <summary>
    /// Serial number of the charging point
    /// </summary>
    public string ChargePointSerialNumber { get; set; } = "ECO001234567";

    /// <summary>
    /// Firmware version of the charging point
    /// </summary>
    public string FirmwareVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Heartbeat interval in seconds
    /// </summary>
    public int HeartbeatInterval { get; set; } = 30;

    /// <summary>
    /// Test RFID card to use for transactions
    /// </summary>
    public string TestRfidCard { get; set; } = "abc123";

    /// <summary>
    /// Current transaction ID (if any)
    /// </summary>
    public int? CurrentTransactionId { get; set; }

    /// <summary>
    /// Current meter value in Wh
    /// </summary>
    public int CurrentMeterValue { get; set; } = 0;

    /// <summary>
    /// Number of connectors on this charging point
    /// </summary>
    public int ConnectorCount { get; set; } = 1;

    /// <summary>
    /// Tenant ID for multi-tenant scenarios
    /// </summary>
    public Guid? TenantId { get; set; }
}