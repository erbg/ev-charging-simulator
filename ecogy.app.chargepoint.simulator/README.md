# OCPP Charging Point Simulator

Dieses Projekt simuliert eine OCPP 1.6 Ladestation, die sich über WebSocket mit Ihrem OCPP-Server verbindet.

## Funktionen

- **Vollständige OCPP 1.6 Simulation**
  - BootNotification beim Start
  - Periodische Heartbeats
  - StatusNotifications
  - MeterValues während Ladevorgängen
  - StartTransaction/StopTransaction Nachrichten
  - Authorize Nachrichten

- **Unterstützte Server-Befehle**
  - RemoteStartTransaction
  - RemoteStopTransaction
  - Reset
  - GetConfiguration
  - ChangeConfiguration

- **Automatische Simulation**
  - Periodische Heartbeats (30s)
  - StatusNotifications (60s)
  - MeterValues während Ladevorgängen (30s)

## Verwendung

### Einfacher Start
```bash
dotnet run
```

### Mit benutzerdefinierten Parametern
```bash
dotnet run -- "ws://localhost:5000/ocpp" "CP002" "def456"
```

Parameter:
1. Server URL (Standard: `ws://localhost:5000/ocpp`)
2. Charging Point ID (Standard: `CP001`)
3. Test RFID Card (Standard: `abc123`)

## Test-Szenarien

### 1. Grundlegende Verbindung testen
```bash
dotnet run
```
- Die Ladestation sendet BootNotification
- Periodische Heartbeats werden gesendet
- StatusNotifications zeigen "Available" Status

### 2. Ladevorgang simulieren
Über Ihre API einen RemoteStartTransaction Befehl senden:
```bash
# Beispiel API Call (anpassen an Ihre Endpoints)
curl -X POST "https://localhost:5001/api/charging/remote-start" \
  -H "Content-Type: application/json" \
  -d '{"chargePointId": "CP001", "idTag": "abc123", "connectorId": 1}'
```

Die Ladestation wird:
1. RemoteStartTransaction akzeptieren
2. StartTransaction Nachricht senden
3. Periodische MeterValues senden
4. Status auf "Charging" ändern

### 3. Ladevorgang beenden
Über Ihre API einen RemoteStopTransaction Befehl senden:
```bash
curl -X POST "https://localhost:5001/api/charging/remote-stop" \
  -H "Content-Type: application/json" \
  -d '{"chargePointId": "CP001", "transactionId": 123}'
```

Die Ladestation wird:
1. RemoteStopTransaction akzeptieren
2. StopTransaction Nachricht senden
3. Status auf "Available" ändern

## Konfiguration

Die `ChargingPointConfiguration` Klasse ermöglicht die Anpassung verschiedener Parameter:

```csharp
var config = new ChargingPointConfiguration
{
    ServerUrl = "ws://localhost:5000/ocpp",
    ChargePointId = "CP001",
    ChargePointModel = "ECO-DC-50",
    ChargePointVendor = "Ecogy",
    HeartbeatInterval = 30,
    TestRfidCard = "abc123"
};
```

## Logging

Das Projekt verwendet Microsoft.Extensions.Logging für ausführliche Protokollierung:
- Verbindungsstatus
- Gesendete/empfangene Nachrichten
- Fehlerbehandlung
- Transaktionsdetails

## Erweiterte Nutzung

### Mehrere Ladestationen simulieren
Starten Sie mehrere Instanzen mit verschiedenen IDs:

```bash
# Terminal 1
dotnet run -- "ws://localhost:5000/ocpp" "CP001" "abc123"

# Terminal 2  
dotnet run -- "ws://localhost:5000/ocpp" "CP002" "def456"

# Terminal 3
dotnet run -- "ws://localhost:5000/ocpp" "CP003" "ghi789"
```

### Integration mit Unit Tests
Sie können den Simulator auch programmatisch in Tests verwenden:

```csharp
var config = new ChargingPointConfiguration
{
    ServerUrl = "ws://localhost:5000/ocpp",
    ChargePointId = "TEST_CP001"
};

var simulator = new ChargingPointSimulator(logger, config);
await simulator.StartAsync(cancellationToken);

// Transaktionen simulieren
await simulator.SimulateTransaction("test123");
```

## Fehlerbehebung

### Verbindungsprobleme
- Stellen Sie sicher, dass Ihr OCPP-Server läuft
- Überprüfen Sie die WebSocket-URL
- Kontrollieren Sie Firewall-Einstellungen

### OCPP-Protokoll-Probleme
- Überprüfen Sie die Logs für Details
- Validieren Sie OCPP-Nachrichten gegen die Spezifikation
- Stellen Sie sicher, dass der Server OCPP 1.6 unterstützt

## Abhängigkeiten

- .NET 9.0
- System.Net.WebSockets.Client
- Newtonsoft.Json
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Logging