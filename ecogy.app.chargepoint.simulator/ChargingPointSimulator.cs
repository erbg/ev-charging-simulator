using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace ecogy.app.chargepoint.simulator;

/// <summary>
/// Simulates an OCPP 1.6 charging station that connects via WebSocket to test the OCPP service
/// </summary>
public class ChargingPointSimulator : BackgroundService
{
    private readonly ILogger<ChargingPointSimulator> _logger;
    private readonly ChargingPointConfiguration _config;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly Random _random = new();
    private readonly Dictionary<string, TaskCompletionSource<JsonElement>> _pendingRequests = new();

    public ChargingPointSimulator(ILogger<ChargingPointSimulator> logger, ChargingPointConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Charging Point Simulator for {ChargePointId}", _config.ChargePointId);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndSimulate(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in charging point simulation");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConnectAndSimulate(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        try
        {
            await ConnectWebSocket(cancellationToken);
            
            // Start listening for incoming messages
            var listenTask = ListenForMessages(_cancellationTokenSource.Token);
            
            // Send initial BootNotification
            await SendBootNotification();
            
            // Wait a moment for BootNotification response
            await Task.Delay(1000, cancellationToken);
            
            // Send initial StatusNotification for connector 0 (charging station)
            await SendStatusNotificationForConnector(0, "Available");
            
            // Send initial StatusNotification for connector 1 (charging connector)
            await SendStatusNotificationForConnector(1, "Available");
            
            // Simulate charging station behavior
            await SimulateChargingStationBehavior(_cancellationTokenSource.Token);
            
            await listenTask;
        }
        finally
        {
            await DisconnectWebSocket();
        }
    }

    private async Task ConnectWebSocket(CancellationToken cancellationToken)
    {
        _webSocket = new ClientWebSocket();
        _webSocket.Options.AddSubProtocol("ocpp1.6");
        
        var uri = new Uri($"{_config.ServerUrl}/{_config.ChargePointId}");
        _logger.LogInformation("Connecting to {Uri}", uri);
        
        await _webSocket.ConnectAsync(uri, cancellationToken);
        _logger.LogInformation("Connected to OCPP server");
    }

    private async Task DisconnectWebSocket()
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Simulation ended", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing WebSocket connection");
            }
        }
        
        _webSocket?.Dispose();
        _webSocket = null;
    }

    private async Task ListenForMessages(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 4];
        
        while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
                    _logger.LogInformation("[{Timestamp}] Received WebSocket message: {Message}", timestamp, message);
                    
                    // Process incoming message in a separate task so we don't block message reception
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessIncomingMessage(message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message in background task: {Message}", message);
                        }
                    }, cancellationToken);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket connection closed by server");
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving WebSocket message");
                break;
            }
        }
    }

    private async Task ProcessIncomingMessage(string message)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(message);
            var root = jsonDoc.RootElement;
            
            if (!root.ValueKind.Equals(JsonValueKind.Array) || root.GetArrayLength() < 3)
                return;

            var messageType = root[0].GetInt32();
            var uniqueId = root[1].GetString();
            
            _logger.LogInformation("Processing {MessageType} message with ID: {UniqueId}", 
                messageType, uniqueId);

            // Handle different message types
            switch (messageType)
            {
                case 2: // CALL - Request from server
                    var action = root[2].GetString();
                    var payload = root.GetArrayLength() > 3 ? root[3] : (JsonElement?)null;
                    await HandleServerRequest(uniqueId!, action!, payload);
                    break;
                case 3: // CALLRESULT - Response from server
                    await HandleServerResponse(uniqueId!, root[2]);
                    break;
                case 4: // CALLERROR - Error from server
                    var errorCode = root[2].GetString();
                    var errorDescription = root[3].GetString();
                    _logger.LogWarning("Received CALLERROR for {UniqueId}: {ErrorCode} - {ErrorDescription}", 
                        uniqueId, errorCode, errorDescription);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing incoming message: {Message}", message);
        }
    }

    private async Task HandleServerRequest(string uniqueId, string action, JsonElement? payload)
    {
        _logger.LogInformation("Handling server request: {Action}", action);

        object? response = action switch
        {
            "RemoteStartTransaction" => await HandleRemoteStartTransaction(payload),
            "RemoteStopTransaction" => await HandleRemoteStopTransaction(payload),
            "Reset" => await HandleReset(payload),
            "GetConfiguration" => await HandleGetConfiguration(payload),
            "ChangeConfiguration" => await HandleChangeConfiguration(payload),
            _ => null
        };

        if (response != null)
        {
            await SendCallResult(uniqueId, response);
        }
        else
        {
            await SendCallError(uniqueId, "NotSupported", $"Action {action} not supported", "{}");
        }
    }

    private async Task HandleServerResponse(string uniqueId, JsonElement payload)
    {
        _logger.LogInformation("Received response for request {UniqueId}: {Payload}", uniqueId, payload.GetRawText());
        
        // Complete pending request if exists
        if (_pendingRequests.TryGetValue(uniqueId, out var tcs))
        {
            tcs.SetResult(payload);
            _pendingRequests.Remove(uniqueId);
        }
        
        // Handle specific responses if needed
        // For example, BootNotification response might contain heartbeat interval
        if (payload.TryGetProperty("interval", out var intervalElement))
        {
            if (intervalElement.TryGetInt32(out var interval))
            {
                _config.HeartbeatInterval = interval;
                _logger.LogInformation("Updated heartbeat interval to {Interval} seconds", interval);
            }
        }

        if (payload.TryGetProperty("transactionId", out var transactionIdElement))
        {
            if (transactionIdElement.TryGetInt32(out var transactionId))
            {
                _config.CurrentTransactionId = transactionIdElement.GetInt32();
                _logger.LogInformation("Updating transactionId to {transactionId}", transactionId);
            }
        }

    }

    private async Task<object> HandleRemoteStartTransaction(JsonElement? payload)
    {
        _logger.LogInformation("Handling RemoteStartTransaction");
        
        // Parse payload to get idTag and connectorId
        string? idTag = null;
        int connectorId = 1; // Default connector
        
        if (payload.HasValue)
        {
            if (payload.Value.TryGetProperty("idTag", out var idTagElement))
            {
                idTag = idTagElement.GetString();
            }
            
            if (payload.Value.TryGetProperty("connectorId", out var connectorIdElement))
            {
                if (connectorIdElement.TryGetInt32(out var connector))
                {
                    connectorId = connector;
                }
            }
        }

        if (string.IsNullOrEmpty(idTag))
        {
            _logger.LogWarning("RemoteStartTransaction received without valid idTag");
            return new { status = "Rejected" };
        }

        _logger.LogInformation("RemoteStartTransaction with idTag: {IdTag}, connectorId: {ConnectorId}", idTag, connectorId);

        // Update configuration with the new idTag
        _config.TestRfidCard = idTag;
        
        // Start the authorize and transaction sequence in the background
        // This runs asynchronously and doesn't block the response
        _ = Task.Run(async () =>
        {
            try
            {
                // Wait a moment to simulate processing time
                await Task.Delay(1000);

                _logger.LogInformation("Starting background authorize and transaction sequence for idTag: {IdTag}", idTag);
                
                // Step 1: Send Authorize request first
                _logger.LogInformation("Sending Authorize request for idTag: {IdTag}", idTag);
                var authorizeResponse = await SendAuthorizeAndWaitForResponse(idTag);
                
                // Check authorize response
                if (authorizeResponse != null && 
                    authorizeResponse.Value.TryGetProperty("idTagInfo", out var idTagInfo) &&
                    idTagInfo.TryGetProperty("status", out var statusElement))
                {
                    var authStatus = statusElement.GetString();
                    _logger.LogInformation("Authorize response status: {Status}", authStatus);
                    
                    if (authStatus == "Accepted")
                    {
                        // Step 2: Authorization successful, now send StartTransaction
                        _logger.LogInformation("Authorization successful, sending StartTransaction");
                        
                        // Wait a moment to simulate processing time
                        await Task.Delay(500);
                        
                        // Send StartTransaction message
                        await SendStartTransactionForConnector(connectorId, idTag);
                        
                        _logger.LogInformation("Background authorize and transaction sequence completed successfully");
                    }
                    else
                    {
                        _logger.LogWarning("Authorization failed with status: {Status} - transaction will not be started", authStatus);
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid or missing authorize response - transaction will not be started");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background authorize and transaction sequence");
            }
        });
        
        // Return immediate response as per OCPP 1.6 specification
        _logger.LogInformation("Sending immediate Accepted response to RemoteStartTransaction");
        return new { status = "Accepted" };
    }

    private async Task<JsonElement?> SendAuthorizeAndWaitForResponse(string idTag)
    {
        var uniqueId = GenerateUniqueId();
        var tcs = new TaskCompletionSource<JsonElement>();
        _pendingRequests[uniqueId] = tcs;
        
        try
        {
            var payload = new { idTag };
            var message = new object[] { 2, uniqueId, "Authorize", payload };
            var json = JsonSerializer.Serialize(message);
            
            await SendMessage(json);
            _logger.LogInformation("Sent Authorize message: {UniqueId}", uniqueId);
            
            // The server seems to consistently take 45+ seconds, so use a 70-second timeout
            var responseTask = tcs.Task;
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(70));
            
            var completedTask = await Task.WhenAny(responseTask, timeoutTask);
            
            if (completedTask == responseTask)
            {
                _logger.LogInformation("Received Authorize response for uniqueId: {UniqueId}", uniqueId);
                return await responseTask;
            }
            else
            {
                _logger.LogWarning("Authorize request timed out after 70 seconds for uniqueId: {UniqueId}", uniqueId);
                
                // Check one final time if the response just completed
                if (responseTask.IsCompleted)
                {
                    _logger.LogInformation("Authorize response completed right at timeout for uniqueId: {UniqueId}", uniqueId);
                    return await responseTask;
                }
                
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Authorize request");
            return null;
        }
        finally
        {
            // Remove the pending request to prevent memory leaks
            _pendingRequests.Remove(uniqueId);
        }
    }

    private async Task SendStartTransactionForConnector(int connectorId, string idTag)
    {
        // Generate a transaction ID
        _config.CurrentTransactionId = _random.Next(100, 9999);
        _config.CurrentMeterValue = _random.Next(1000, 5000);

        var payload = new
        {
            connectorId = connectorId,
            idTag = idTag,
            meterStart = _config.CurrentMeterValue,
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
        
        await SendCall("StartTransaction", payload);
        _logger.LogInformation("Started transaction {TransactionId} with meter value {MeterValue} on connector {ConnectorId}", 
            _config.CurrentTransactionId, _config.CurrentMeterValue, connectorId);
    }

    private async Task<object> HandleRemoteStopTransaction(JsonElement? payload)
    {
        _logger.LogInformation("Handling RemoteStopTransaction");
        
        int? transactionId = null;
        
        // Parse payload to get transactionId
        if (payload.HasValue && payload.Value.TryGetProperty("transactionId", out var transactionIdElement))
        {
            if (transactionIdElement.TryGetInt32(out var txId))
            {
                transactionId = txId;
                _logger.LogInformation("RemoteStopTransaction for transactionId: {TransactionId}", transactionId);
            }
        }

        if (!transactionId.HasValue)
        {
            _logger.LogWarning("RemoteStopTransaction received without valid transactionId");
            return new { status = "Rejected" };
        }

        // Check if this matches our current transaction
        if (_config.CurrentTransactionId != transactionId)
        {
            _logger.LogWarning("RemoteStopTransaction for transactionId {TransactionId} does not match current transaction {CurrentTransactionId}", 
                transactionId, _config.CurrentTransactionId);
            return new { status = "Rejected" };
        }

        // Start the stop transaction sequence in the background
        // This runs asynchronously and doesn't block the response
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Starting background stop transaction sequence for transactionId: {TransactionId}", transactionId);
                
                // Simulate stopping processing time
                await Task.Delay(1000);
                
                // Send StopTransaction message
                await SendStopTransaction();
                
                _logger.LogInformation("Background stop transaction sequence completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background stop transaction sequence");
            }
        });
        
        // Return immediate response as per OCPP 1.6 specification
        _logger.LogInformation("Sending immediate Accepted response to RemoteStopTransaction");
        return new { status = "Accepted" };
    }

    private async Task<object> HandleReset(JsonElement? payload)
    {
        _logger.LogInformation("Handling Reset request");
        
        // Parse reset type if provided
        if (payload.HasValue && payload.Value.TryGetProperty("type", out var typeElement))
        {
            var resetType = typeElement.GetString();
            _logger.LogInformation("Reset type: {ResetType}", resetType);
        }
        
        return new { status = "Accepted" };
    }

    private async Task<object> HandleGetConfiguration(JsonElement? payload)
    {
        _logger.LogInformation("Handling GetConfiguration request");
        
        // Parse requested keys if provided
        var requestedKeys = new List<string>();
        if (payload.HasValue && payload.Value.TryGetProperty("key", out var keyElement) && keyElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var key in keyElement.EnumerateArray())
            {
                var keyName = key.GetString();
                if (!string.IsNullOrEmpty(keyName))
                    requestedKeys.Add(keyName);
            }
        }
        
        // Return configuration based on requested keys or all if none specified
        var configKeys = new List<object>();
        var unknownKeys = new List<string>();
        
        if (requestedKeys.Count == 0 || requestedKeys.Contains("HeartbeatInterval"))
        {
            configKeys.Add(new { key = "HeartbeatInterval", value = _config.HeartbeatInterval.ToString(), @readonly = false });
        }
        
        if (requestedKeys.Count == 0 || requestedKeys.Contains("MeterValuesSampledData"))
        {
            configKeys.Add(new { key = "MeterValuesSampledData", value = "Energy.Active.Import.Register", @readonly = false });
        }
        
        // Add unknown keys
        foreach (var key in requestedKeys)
        {
            if (key != "HeartbeatInterval" && key != "MeterValuesSampledData")
            {
                unknownKeys.Add(key);
            }
        }
        
        return new 
        { 
            configurationKey = configKeys.ToArray(),
            unknownKey = unknownKeys.ToArray()
        };
    }

    private async Task<object> HandleChangeConfiguration(JsonElement? payload)
    {
        _logger.LogInformation("Handling ChangeConfiguration request");
        
        if (payload.HasValue)
        {
            var key = payload.Value.TryGetProperty("key", out var keyElement) ? keyElement.GetString() : null;
            var value = payload.Value.TryGetProperty("value", out var valueElement) ? valueElement.GetString() : null;
            
            _logger.LogInformation("ChangeConfiguration: {Key} = {Value}", key, value);
            
            // Handle specific configuration changes
            if (key == "HeartbeatInterval" && int.TryParse(value, out var interval))
            {
                _config.HeartbeatInterval = interval;
                _logger.LogInformation("Updated HeartbeatInterval to {Interval} seconds", interval);
                return new { status = "Accepted" };
            }
        }
        
        return new { status = "Rejected" };
    }

    private async Task SimulateChargingStationBehavior(CancellationToken cancellationToken)
    {
        var heartbeatTask = SendPeriodicHeartbeats(cancellationToken);
        var statusTask = SendPeriodicStatusNotifications(cancellationToken);
        var meterValuesTask = SendPeriodicMeterValues(cancellationToken);

        // Wait for cancellation or any task to complete with error
        await Task.WhenAny(heartbeatTask, statusTask, meterValuesTask);
    }

    private async Task SendPeriodicHeartbeats(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.HeartbeatInterval), cancellationToken);
                await SendHeartbeat();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeat");
            }
        }
    }

    private async Task SendPeriodicStatusNotifications(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
                await SendStatusNotification();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending status notification");
            }
        }
    }

    private async Task SendPeriodicMeterValues(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                if (_config.CurrentTransactionId.HasValue)
                {
                    await SendMeterValues();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending meter values");
            }
        }
    }

    #region OCPP Message Sending Methods

    private async Task SendBootNotification()
    {
        var payload = new
        {
            chargePointModel = _config.ChargePointModel,
            chargePointVendor = _config.ChargePointVendor,
            chargePointSerialNumber = _config.ChargePointSerialNumber,
            firmwareVersion = _config.FirmwareVersion
        };
        
        await SendCall("BootNotification", payload);
    }

    private async Task SendHeartbeat()
    {
        await SendCall("Heartbeat", new { });
    }

    private async Task SendStatusNotification()
    {
        var payload = new
        {
            connectorId = 1,
            errorCode = "NoError",
            status = _config.CurrentTransactionId.HasValue ? "Charging" : "Available",
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
        
        await SendCall("StatusNotification", payload);
    }

    private async Task SendAuthorize(string idTag)
    {
        var payload = new { idTag };
        await SendCall("Authorize", payload);
    }

    private async Task SendStartTransaction()
    {
        // Use the new method with default connector 1
        await SendStartTransactionForConnector(1, _config.TestRfidCard);
    }

    private async Task SendStopTransaction()
    {
        if (!_config.CurrentTransactionId.HasValue) return;

        // Simulate energy consumption
        var energyConsumed = _random.Next(500, 5000);
        _config.CurrentMeterValue += energyConsumed;

        var payload = new
        {
            idTag = _config.TestRfidCard,
            transactionId = _config.CurrentTransactionId.Value,
            meterStop = _config.CurrentMeterValue,
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            reason = "Remote"
        };
        
        await SendCall("StopTransaction", payload);
        _logger.LogInformation("Stopped transaction {TransactionId} with final meter value {MeterValue} (consumed: {Energy})", 
            _config.CurrentTransactionId, _config.CurrentMeterValue, energyConsumed);
        
        _config.CurrentTransactionId = null;
    }

    private async Task SendMeterValues()
    {
        if (!_config.CurrentTransactionId.HasValue) return;

        // Simulate meter value increase
        _config.CurrentMeterValue += _random.Next(1, 10);

        var payload = new
        {
            connectorId = 1,
            transactionId = _config.CurrentTransactionId.Value,
            meterValue = new[]
            {
                new
                {
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    sampledValue = new[]
                    {
                        new
                        {
                            value = _config.CurrentMeterValue.ToString(),
                            context = "Sample.Periodic",
                            measurand = "Energy.Active.Import.Register",
                            unit = "Wh"
                        }
                    }
                }
            }
        };
        
        await SendCall("MeterValues", payload);
    }

    private async Task SendCall(string action, object payload)
    {
        var uniqueId = GenerateUniqueId();
        var message = new object[] { 2, uniqueId, action, payload };
        var json = JsonSerializer.Serialize(message);
        _pendingRequests.Add(uniqueId, new TaskCompletionSource<JsonElement>());

        await SendMessage(json);
        _logger.LogInformation("Sent {Action} message: {UniqueId}", action, uniqueId);
    }

    private async Task SendCallResult(string uniqueId, object payload)
    {
        var message = new object[] { 3, uniqueId, payload };
        var json = JsonSerializer.Serialize(message);
        
        await SendMessage(json);
        _logger.LogInformation("Sent CALLRESULT for: {UniqueId}", uniqueId);
    }

    private async Task SendCallError(string uniqueId, string errorCode, string errorDescription, string errorDetails)
    {
        var message = new object[] { 4, uniqueId, errorCode, errorDescription, errorDetails };
        var json = JsonSerializer.Serialize(message);
        
        await SendMessage(json);
        _logger.LogWarning("Sent CALLERROR for: {UniqueId} - {ErrorCode}: {ErrorDescription}", 
            uniqueId, errorCode, errorDescription);
    }

    private async Task SendMessage(string message)
    {
        if (_webSocket?.State == WebSocketState.Open)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
            
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            _logger.LogInformation("[{Timestamp}] Sent WebSocket message: {Message}", timestamp, message);
        }
        else
        {
            _logger.LogWarning("Cannot send message - WebSocket not connected");
        }
    }

    private async Task SendStatusNotificationForConnector(int connectorId, string status)
    {
        var payload = new
        {
            connectorId = connectorId,
            errorCode = "NoError",
            status = status,
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
        
        await SendCall("StatusNotification", payload);
        _logger.LogInformation("Sent StatusNotification for connector {ConnectorId} with status {Status}", connectorId, status);
    }

    #endregion

    #region Public Control Methods

    public async Task SimulateAuthorization(string idTag)
    {
        _logger.LogInformation("Simulating authorization for RFID card: {IdTag}", idTag);
        await SendAuthorize(idTag);
    }

    public async Task SimulateTransaction(string idTag)
    {
        _logger.LogInformation("Simulating complete transaction for RFID card: {IdTag}", idTag);
        
        // First authorize and wait for response
        var authorizeResponse = await SendAuthorizeAndWaitForResponse(idTag);
        
        if (authorizeResponse != null && 
            authorizeResponse.Value.TryGetProperty("idTagInfo", out var idTagInfo) &&
            idTagInfo.TryGetProperty("status", out var statusElement) &&
            statusElement.GetString() == "Accepted")
        {
            _logger.LogInformation("Authorization successful, starting transaction");
            
            // Start transaction
            _config.TestRfidCard = idTag;
            await SendStartTransactionForConnector(1, idTag);
            await Task.Delay(5000);
            
            // Send some meter values
            for (int i = 0; i < 3; i++)
            {
                await SendMeterValues();
                await Task.Delay(2000);
            }
            
            // Stop transaction
            await SendStopTransaction();
        }
        else
        {
            _logger.LogWarning("Authorization failed or timed out for RFID card: {IdTag}", idTag);
        }
    }

    public async Task SimulateConnectorStatusChange(string status)
    {
        _logger.LogInformation("Simulating connector status change to: {Status}", status);
        
        var payload = new
        {
            connectorId = 1,
            errorCode = "NoError",
            status = status,
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
        
        await SendCall("StatusNotification", payload);
    }

    #endregion

    private string GenerateUniqueId()
    {
        return DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + _random.Next(100, 999).ToString("D3");
    }

    public override void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _webSocket?.Dispose();
        base.Dispose();
    }
}