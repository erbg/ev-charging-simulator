# OCPP Charging Point Simulator & Test Suite

Dieses Projekt enthält einen vollständigen OCPP 1.6 Ladestation-Simulator und Testsuite für die Integration mit Ihrem OCPP-Server.

## Projektstruktur

```
ecogy.app.chargepoint.simulator/    # Hauptsimulator-Projekt (Automatisch)
??? ChargingPointSimulator.cs           # Kern-Simulator-Klasse
??? ChargingPointConfiguration.cs       # Konfiguration
??? Program.cs                          # Automatischer Modus (Top-Level)
??? start-simulator.sh/bat              # Start-Skripte
??? README.md                           # Simulator-Dokumentation

ecogy.app.chargepoint.interactive/  # Interaktives Simulator-Projekt
??? Program.cs                          # Interaktiver Modus (Top-Level)
??? InteractiveSimulator.cs             # Interaktive Konsolen-Steuerung
??? README.md                           # Interactive-Dokumentation

ecogy.app.chargepoint.tests/        # Integrationstests
??? ChargingPointIntegrationTests.cs    # Test-Szenarien
??? run-tests.sh/bat                    # Test-Runner-Skripte
??? README.md                           # Test-Dokumentation
```

## Schnellstart

### 1. Automatischer Simulator
```bash
cd ecogy.app.chargepoint.simulator
dotnet run
```

### 2. Interaktiver Simulator
```bash
cd ecogy.app.chargepoint.interactive
dotnet run
```

### 3. Start-Skript verwenden
```bash
cd ecogy.app.chargepoint.simulator
./start-simulator.sh  # Linux/Mac
# oder
start-simulator.bat   # Windows
# Wählen Sie Modus 1 (Automatisch) oder 2 (Interaktiv)
```

### 4. Tests ausführen
```bash
cd ecogy.app.chargepoint.tests
./run-tests.sh        # Linux/Mac
# oder
run-tests.bat         # Windows
```

## Hauptfunktionen

### OCPP 1.6 Simulator
- ? **WebSocket-Verbindung** zu OCPP-Server
- ? **BootNotification** beim Start
- ? **Periodische Heartbeats** (konfigurierbar)
- ? **StatusNotifications** für Connector-Status
- ? **Authorize** für RFID-Karten-Validierung
- ? **StartTransaction** / **StopTransaction** 
- ? **MeterValues** während Ladevorgängen
- ? **RemoteStartTransaction** / **RemoteStopTransaction** Unterstützung
- ? **GetConfiguration** / **ChangeConfiguration**
- ? **Reset** Kommando

### Modi
1. **Automatischer Modus** (`ecogy.app.chargepoint.simulator`): 
   - Läuft im Hintergrund
   - Sendet periodische Nachrichten automatisch
   - Ideal für Dauertests und CI/CD

2. **Interaktiver Modus** (`ecogy.app.chargepoint.interactive`): 
   - Manuelle Kontrolle über Konsolen-Befehle
   - Ideal für Debugging und manuelle Tests
   - Schritt-für-Schritt Simulation

### Konfiguration
```csharp
var config = new ChargingPointConfiguration
{
    ServerUrl = "ws://localhost:5000/ocpp",      // OCPP Server URL
    ChargePointId = "CP001",                     // Eindeutige Ladestation-ID
    ChargePointModel = "ECO-DC-50",              // Modell
    ChargePointVendor = "Ecogy",                 // Hersteller
    HeartbeatInterval = 30,                      // Heartbeat-Intervall (Sekunden)
    TestRfidCard = "abc123"                      // Test-RFID-Karte
};
```

## Verwendungsszenarien

### 1. Entwicklung & Debugging
```bash
# Terminal 1: OCPP Server starten
dotnet run --project ecogy.app.api

# Terminal 2: Interaktiven Simulator starten  
cd ecogy.app.chargepoint.interactive
dotnet run -- "ws://localhost:5000/ocpp" "DEV_CP001" "dev123"

# Verwenden Sie Kommandos:
# 1 - RFID autorisieren
# 2 - Komplette Transaktion
# 3 - Status ändern
```

### 2. Automatisierte Tests
```bash
# Hintergrund-Simulator für Tests
cd ecogy.app.chargepoint.simulator  
dotnet run -- "ws://localhost:5000/ocpp" "TEST_CP001" "test123" &

# Integrationstests ausführen
cd ecogy.app.chargepoint.tests
dotnet test

# Spezifische Tests
dotnet test --filter "FullyQualifiedName~Transaction"
```

### 3. Lasttests (Mehrere Ladestationen)
```bash
# Terminal 1 - Automatischer Modus
cd ecogy.app.chargepoint.simulator
dotnet run -- "ws://localhost:5000/ocpp" "CP001" "card001" &

# Terminal 2 - Automatischer Modus
dotnet run -- "ws://localhost:5000/ocpp" "CP002" "card002" &

# Terminal 3 - Interaktiver Modus für Kontrolle
cd ecogy.app.chargepoint.interactive
dotnet run -- "ws://localhost:5000/ocpp" "CP003" "card003"
```

### 4. CI/CD Integration
```yaml
# Azure DevOps Pipeline Beispiel
- task: DotNetCoreCLI@2
  displayName: 'Start OCPP Simulator'
  inputs:
    command: 'run'
    projects: 'ecogy.app.chargepoint.simulator/ecogy.app.chargepoint.simulator.csproj'
    arguments: '-- "ws://localhost:5000/ocpp" "CI_CP001" "ci_test"'
  condition: succeededOrFailed()

- task: DotNetCoreCLI@2
  displayName: 'Run Integration Tests'
  inputs:
    command: 'test'
    projects: 'ecogy.app.chargepoint.tests/ecogy.app.chargepoint.tests.csproj'
```

## API-Integration Testen

### RemoteStartTransaction testen
```bash
# Simulator starten (automatisch oder interaktiv)
dotnet run -- "ws://localhost:5000/ocpp" "CP001" "abc123"

# In anderem Terminal: API-Aufruf
curl -X POST "https://localhost:5001/api/charging/remote-start" \
  -H "Content-Type: application/json" \
  -d '{"chargePointId": "CP001", "idTag": "abc123", "connectorId": 1}'
```

### RemoteStopTransaction testen  
```bash
# Nach StartTransaction:
curl -X POST "https://localhost:5001/api/charging/remote-stop" \
  -H "Content-Type: application/json" \
  -d '{"chargePointId": "CP001", "transactionId": 123}'
```

## Architektur

### Projekt-Abhängigkeiten
```
ecogy.app.chargepoint.interactive
??? ecogy.app.chargepoint.simulator (Referenz)
    ??? ecogy.app.api (Referenz)

ecogy.app.chargepoint.tests  
??? ecogy.app.chargepoint.simulator (Referenz)
??? ecogy.app.api (Referenz)
```

### Warum zwei Projekte?
- **.NET erlaubt nur ein Top-Level-Programm pro Projekt**
- **Automatischer Modus**: Eigenständiger Hintergrundservice
- **Interaktiver Modus**: Benutzeroberfläche mit manueller Kontrolle
- **Gemeinsame Klassen**: `ChargingPointSimulator` und `ChargingPointConfiguration` sind geteilt

## Nachrichten-Flow

### Normale Verbindung
1. **WebSocket Connect** ? Server
2. **BootNotification** ? Server
3. **BootNotification Response** ? Server
4. **Heartbeat** (alle 30s) ? Server
5. **StatusNotification** (periodisch) ? Server

### Transaktion (Automatisch)
1. **Authorize** ? Server (bei RemoteStartTransaction)
2. **StartTransaction** ? Server
3. **MeterValues** ? Server (alle 30s während Ladung)
4. **StopTransaction** ? Server (bei RemoteStopTransaction)

### Transaktion (Interaktiv)
```
Enter command: 2
Enter RFID card ID: test123
? Authorize (test123)
? Authorize Response (Accepted)
? StartTransaction (test123, connector=1)
? StartTransaction Response (transactionId=123)
? MeterValues (3x mit steigenden Werten)
? StopTransaction (transactionId=123, reason=Remote)
? StopTransaction Response
```

## Debugging & Monitoring

### Logs aktivieren
```bash
# Detaillierte Logs (automatisch)
dotnet run -- "ws://localhost:5000/ocpp" "CP001" "abc123"

# Interaktive Logs
dotnet run -- "ws://localhost:5000/ocpp" "CP001" "abc123"
# Alle Kommandos und Responses werden angezeigt
```

### Nachrichten-Format
```json
// CALL (Client ? Server)
[2, "20241201123456789", "StartTransaction", {
  "connectorId": 1,
  "idTag": "abc123", 
  "meterStart": 147,
  "timestamp": "2024-12-01T12:34:56.789Z"
}]

// CALLRESULT (Server ? Client)  
[3, "20241201123456789", {
  "transactionId": 123,
  "idTagInfo": {"status": "Accepted"}
}]
```

## Troubleshooting

### Verbindungsprobleme
- ? OCPP-Server läuft auf korrekter URL
- ? WebSocket-Protokoll "ocpp1.6" wird unterstützt  
- ? Firewall-Einstellungen erlauben Verbindung
- ? Kein anderer Simulator mit gleicher ChargePointId

### Build-Probleme
- ? .NET 9.0 SDK installiert
- ? NuGet-Pakete wiederhergestellt (`dotnet restore`)
- ? Projektabhängigkeiten korrekt

### OCPP-Protokoll-Fehler
- ? Nachrichten entsprechen OCPP 1.6 Spezifikation
- ? JSON-Format ist korrekt
- ? Pflichtfelder sind vorhanden
- ? Server sendet korrekte Responses

## Vorteile für Ihr Projekt

1. **Vollständige OCPP 1.6 Simulation** - Testen Sie alle Aspekte Ihrer OCPP-Implementation
2. **Keine Hardware erforderlich** - Entwickeln und testen Sie ohne physische Ladestationen
3. **Flexible Modi** - Automatisch für Tests, interaktiv für Debugging
4. **Automatisierte Tests** - Integrationstests für CI/CD-Pipeline
5. **Skalierbare Tests** - Simulieren Sie mehrere Ladestationen gleichzeitig
6. **Realistische Szenarien** - Vollständige Transaktions-Zyklen mit MeterValues

Das Projekt ist vollständig funktionsfähig und kann sofort verwendet werden! ??