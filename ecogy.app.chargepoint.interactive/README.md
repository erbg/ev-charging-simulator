# OCPP Charging Point Interactive Simulator

Dieses Projekt stellt eine interaktive Konsolenanwendung zur manuellen Steuerung der OCPP Ladestation-Simulation bereit.

## Funktionen

- **Manuelle Kontrolle** �ber alle OCPP-Nachrichten
- **Interaktive Kommandos** f�r verschiedene Szenarien
- **Echtzeit-Feedback** �ber gesendete/empfangene Nachrichten
- **Benutzerfreundliche Men�s** f�r alle Aktionen

## Verwendung

### Direkter Start
```bash
dotnet run
```

### Mit Parametern
```bash
dotnet run -- "ws://localhost:5000/ocpp" "CP001" "abc123"
```

### �ber Start-Skript
```bash
# Von simulator-Verzeichnis aus
./start-simulator.sh
# W�hlen Sie Option 2 f�r interaktiven Modus
```

## Verf�gbare Kommandos

### 1 - Authorize RFID card
Sendet eine Authorize-Anfrage zur RFID-Validierung:
- Eingabe einer benutzerdefinierten RFID-Karte oder Verwendung der Standard-Karte
- Zeigt Server-Response an

### 2 - Simulate complete transaction
F�hrt einen kompletten Ladevorgang aus:
1. **Authorize** - Autorisiert die RFID-Karte
2. **StartTransaction** - Startet die Transaktion
3. **MeterValues** - Sendet Z�hlerwerte (3x)
4. **StopTransaction** - Beendet die Transaktion

### 3 - Change connector status
�ndert den Connector-Status:
- Available
- Preparing
- Charging
- SuspendedEVSE
- SuspendedEV
- Finishing
- Reserved
- Unavailable
- Faulted

### 4 - Show current configuration
Zeigt aktuelle Simulator-Einstellungen an:
- Server URL
- Charging Point ID
- Hardware-Informationen
- Aktuelle Transaktions-Details

### h - Show help
Zeigt detaillierte Hilfe zu allen Kommandos

### q - Quit
Beendet den Simulator sauber

## Beispiel-Session

```
=== Interactive OCPP Charging Point Simulator ===
Charging Point ID: CP001
Available Commands:
  1 - Authorize RFID card
  2 - Simulate complete transaction
  3 - Change connector status
  4 - Show current configuration
  h - Show help
  q - Quit
=================================================

Enter command: 2
Enter RFID card ID (or press Enter for default 'abc123'): test123
Starting complete transaction simulation for card: test123
This will:
  1. Send Authorize request
  2. Start transaction
  3. Send meter values
  4. Stop transaction
Please wait...
Transaction simulation completed!

Enter command: 3
Available connector statuses:
  1 - Available
  2 - Preparing
  3 - Charging
  ...
Select status (1-9): 1
Status notification sent: Available

Enter command: q
```

## Integration in Tests

Das interaktive Projekt kann auch programmatisch in Tests verwendet werden:

```csharp
// Host f�r interaktive Simulation erstellen
var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services => {
        services.AddSingleton(config);
        services.AddSingleton<ChargingPointSimulator>();
        services.AddTransient<InteractiveSimulator>();
    })
    .Build();

// Simulator starten und Kommandos ausf�hren
var simulator = host.Services.GetRequiredService<ChargingPointSimulator>();
await simulator.StartAsync(cancellationToken);

var interactive = host.Services.GetRequiredService<InteractiveSimulator>();
// Kommandos k�nnen programmatisch getriggert werden
```

## Architektur

Das interaktive Projekt:
- Referenziert das Hauptsimulator-Projekt
- Verwendet dieselben Konfigurationsklassen
- L�uft als separater Prozess mit eigener Main-Methode
- Kommuniziert �ber die gleichen WebSocket-Verbindungen

## Debugging

### Detaillierte Logs aktivieren
```bash
dotnet run -- "ws://localhost:5000/ocpp" "CP001" "abc123" --verbose
```

### WebSocket-Nachrichten verfolgen
Alle gesendeten und empfangenen OCPP-Nachrichten werden in der Konsole angezeigt mit:
- Timestamp
- Nachrichtentyp (CALL, CALLRESULT, CALLERROR)
- Action
- Payload-Details

### Fehlerbehandlung
- Ung�ltige Eingaben werden abgefangen
- Verbindungsfehler werden angezeigt
- Simulator kann jederzeit mit 'q' beendet werden