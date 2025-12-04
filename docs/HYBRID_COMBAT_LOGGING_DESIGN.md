# Hybrid Combat Logging - Toggle-basiert

**Konzept:** User entscheidet selbst zwischen Fast Mode (default) und Advanced Mode
**Best of Both Worlds:** Performance + Flexibilität

---

## 🎯 Zwei Modi

### Mode 1: **Standard Combat Analysis** (DEFAULT)

```
☐ Advanced Combat Logging

→ Wie bisher:
  - Nur aggregierte Stats in SQLite
  - Schnell, wenig Disk Space
  - Charts mit 500 Punkten
  - KEINE BSON-Dateien
```

**Perfekt für:**
- ✅ Casual Gameplay
- ✅ Schnelle Übersicht
- ✅ Keine Performance-Einbußen

---

### Mode 2: **Advanced Combat Logging** (OPT-IN)

```
☑ Advanced Combat Logging ← User aktiviert

→ Zusätzlich:
  - Packet-Level-Events in BSON
  - Rolling Window (max 10 Encounters)
  - Vollständige Replay-Fähigkeit
  - Export/Share möglich
```

**Perfekt für:**
- ✅ Wichtige Boss-Fights
- ✅ Detaillierte Analyse
- ✅ Teilen mit Guild
- ✅ Später nochmal angucken

---

## 🎨 Settings UI

### Einfache Variante

```
┌─────────────────────────────────────────────────┐
│  ⚙️ Combat Logging                               │
├─────────────────────────────────────────────────┤
│                                                 │
│  ☐ Advanced Combat Logging                     │
│                                                 │
│     When enabled, detailed packet-level data    │
│     will be saved for replay and analysis.      │
│     Uses ~450 MB for last 10 encounters.        │
│                                                 │
│  ┌───────────────────────────────────────────┐ │
│  │  Max Stored Encounters: [10      ] ▼     │ │
│  │  (Only when Advanced Logging is enabled) │ │
│  └───────────────────────────────────────────┘ │
│                                                 │
│  [ View Stored Logs (0) ]  [ Clear All ]       │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Erweiterte Variante mit Info

```
┌──────────────────────────────────────────────────────────┐
│  ⚙️ Combat Logging                                        │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  Combat Analysis Mode:                                   │
│                                                          │
│  ○ Standard (Fast)                    ← DEFAULT         │
│     Aggregated stats only                               │
│     Low disk usage (~5 MB/session)                      │
│     No replay capability                                │
│                                                          │
│  ● Advanced (Detailed)                ← USER AKTIVIERT   │
│     Packet-level event recording                        │
│     Full replay capability                              │
│     Higher disk usage (~45 MB/encounter)                │
│                                                          │
│  ┌────────────────────────────────────────────────────┐ │
│  │  Advanced Settings (when enabled):                │ │
│  │                                                    │ │
│  │  Max Stored Encounters: [10  ▼]                   │ │
│  │  • 5  → ~225 MB                                   │ │
│  │  • 10 → ~450 MB  ← Recommended                    │ │
│  │  • 20 → ~900 MB                                   │ │
│  │                                                    │ │
│  │  Current Usage: 0/10 encounters (0 MB)            │ │
│  │                                                    │ │
│  │  [ View Stored Logs ]  [ Clear All ]              │ │
│  └────────────────────────────────────────────────────┘ │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

---

## 🔧 Implementation

### 1. AppSettings Erweiterung

```csharp
// BlueMeter.Core/Configuration/AppSettings.cs

public class AppSettings
{
    /// <summary>
    /// Enable advanced packet-level combat logging
    /// When disabled (default), only aggregated stats are saved
    /// </summary>
    public bool EnableAdvancedCombatLogging { get; set; } = false;

    /// <summary>
    /// Maximum number of encounters to store in advanced mode
    /// Oldest encounters are automatically deleted
    /// </summary>
    public int MaxStoredEncounters { get; set; } = 10;

    /// <summary>
    /// Custom directory for battle logs (null = default)
    /// </summary>
    public string? BattleLogDirectory { get; set; } = null;
}
```

---

### 2. Conditional Initialization

```csharp
// BlueMeter.Core/Data/DataStorageExtensions.cs

private static BattleLogManager? _battleLogManager;
private static BattleLogRecorder? _battleLogRecorder;
private static AppSettings? _appSettings;

public static async Task InitializeDatabaseAsync(
    IDataStorage? dataStorage = null,
    string? databasePath = null,
    object? chartDataService = null,
    AppSettings? appSettings = null,
    ...)
{
    _appSettings = appSettings;

    // ... existing initialization (SQLite, etc.)

    // Initialize advanced logging ONLY if enabled
    if (_appSettings?.EnableAdvancedCombatLogging == true)
    {
        InitializeAdvancedLogging();
    }
    else
    {
        Console.WriteLine("[DataStorageExtensions] Advanced combat logging disabled (using fast mode)");
    }
}

private static void InitializeAdvancedLogging()
{
    try
    {
        _battleLogManager = new BattleLogManager(
            logDirectory: _appSettings?.BattleLogDirectory,
            maxEncounters: _appSettings?.MaxStoredEncounters ?? 10
        );

        _battleLogRecorder = new BattleLogRecorder(_dataStorage);
        _battleLogRecorder.Start();

        var encounters = _battleLogManager.GetStoredEncounters();
        var totalSize = _battleLogManager.GetTotalDiskUsageBytes();

        Console.WriteLine($"[DataStorageExtensions] Advanced combat logging ENABLED");
        Console.WriteLine($"  - Max encounters: {_appSettings?.MaxStoredEncounters ?? 10}");
        Console.WriteLine($"  - Currently stored: {encounters.Count} encounters ({FormatBytes(totalSize)})");
        Console.WriteLine($"  - Log directory: {_battleLogManager.LogDirectory}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DataStorageExtensions] ERROR initializing advanced logging: {ex.Message}");
        Console.WriteLine($"  Falling back to standard mode");

        // Cleanup on failure
        _battleLogRecorder?.Dispose();
        _battleLogRecorder = null;
        _battleLogManager = null;
    }
}
```

---

### 3. Conditional Saving

```csharp
private static async void OnNewSectionCreated()
{
    try
    {
        // ✅ ALWAYS: Save to SQLite (aggregated stats)
        if (_encounterService != null && _encounterService.IsEncounterActive)
        {
            await SaveCurrentEncounterAsync();  // SQLite
        }

        // ✅ CONDITIONAL: Save to BSON if advanced logging enabled
        if (_appSettings?.EnableAdvancedCombatLogging == true &&
            _battleLogRecorder != null &&
            _battleLogManager != null)
        {
            await SaveBattleLogAsync();  // BSON
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling new section: {ex.Message}");
    }
}
```

---

### 4. Runtime Toggle Support

**User kann Toggle während App läuft ändern:**

```csharp
public static void OnSettingsChanged(AppSettings newSettings)
{
    var wasEnabled = _appSettings?.EnableAdvancedCombatLogging ?? false;
    var nowEnabled = newSettings.EnableAdvancedCombatLogging;

    _appSettings = newSettings;

    // Detect toggle change
    if (!wasEnabled && nowEnabled)
    {
        // User ENABLED advanced logging
        Console.WriteLine("[DataStorageExtensions] User enabled advanced logging");
        InitializeAdvancedLogging();
    }
    else if (wasEnabled && !nowEnabled)
    {
        // User DISABLED advanced logging
        Console.WriteLine("[DataStorageExtensions] User disabled advanced logging");
        ShutdownAdvancedLogging();
    }
    else if (wasEnabled && nowEnabled)
    {
        // Settings changed but still enabled (e.g., max encounters changed)
        Console.WriteLine("[DataStorageExtensions] Advanced logging settings updated");

        // Recreate manager with new settings
        ShutdownAdvancedLogging();
        InitializeAdvancedLogging();
    }
}

private static void ShutdownAdvancedLogging()
{
    try
    {
        _battleLogRecorder?.Dispose();
        _battleLogRecorder = null;
        _battleLogManager = null;

        Console.WriteLine("[DataStorageExtensions] Advanced logging shutdown complete");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DataStorageExtensions] Error during shutdown: {ex.Message}");
    }
}
```

---

## 📊 Vergleich der Modi

| Aspekt | Standard Mode | Advanced Mode |
|--------|---------------|---------------|
| **Performance** | ✅ Schnellst | ⚠️ ~5% Overhead |
| **Disk Usage/Session** | ✅ ~500 KB | ⚠️ ~45 MB |
| **Total Disk (10 Sessions)** | ✅ ~5 MB | ⚠️ ~450 MB |
| **Aggregierte Stats** | ✅ JA | ✅ JA |
| **Chart History** | ⚠️ 500 Punkte | ✅ Unbegrenzt |
| **Replay** | ❌ NEIN | ✅ JA |
| **Export** | ⚠️ Nur Stats | ✅ Full Events |
| **Post-Analysis** | ❌ NEIN | ✅ JA |

---

## 🎬 User Journey

### Scenario 1: Casual Player

```
1. Installiert BlueMeter
2. Advanced Logging = OFF (default)
3. Spielt normal
4. Sieht DPS-Stats in Echtzeit ✅
5. Charts funktionieren ✅
6. Kein Disk-Space-Problem ✅
```

**Perfekt!** User merkt keinen Unterschied zu vorher.

---

### Scenario 2: Guild Raider (wichtiger Boss)

```
1. Vor Boss-Pull:
   Settings → ☑ Advanced Combat Logging aktivieren

2. Boss-Fight läuft
   → Events werden in BSON gespeichert

3. Nach Boss-Kill:
   Charts Window → "View Stored Logs"
   → Kann Fight komplett abspielen

4. Export:
   → "Export to .bmlogs"
   → Sendet an Guild Leader

5. Danach:
   Settings → ☐ Advanced Logging deaktivieren
   → Normal weiterspielen
```

**Flexibel!** User aktiviert nur wenn benötigt.

---

### Scenario 3: Streamer/Content Creator

```
1. Settings → ☑ Advanced Logging PERMANENT an

2. Alle Encounters werden gespeichert

3. Nach Stream:
   → Öffnet gespeicherte Logs
   → Analysiert Performance
   → Erstellt Highlight-Videos

4. Disk Usage:
   → Max 10 Encounters = ~450 MB
   → Kein Problem für Gaming-PC
```

**Power-User!** Volle Features verfügbar.

---

## 🔔 User Notifications

### Beim ersten Aktivieren

```
┌────────────────────────────────────────────────┐
│  ℹ️ Advanced Combat Logging Enabled            │
├────────────────────────────────────────────────┤
│                                                │
│  Detailed combat events will now be recorded   │
│  for the last 10 encounters.                   │
│                                                │
│  Disk usage: ~450 MB maximum                   │
│                                                │
│  You can view and export stored encounters in  │
│  Settings → View Stored Logs                   │
│                                                │
│  [ OK ]  [ Don't show again ]                  │
└────────────────────────────────────────────────┘
```

### Warnung bei Disk-Space-Problemen

```
┌────────────────────────────────────────────────┐
│  ⚠️ Low Disk Space Warning                     │
├────────────────────────────────────────────────┤
│                                                │
│  Available disk space: 200 MB                  │
│  Advanced logging requires: ~450 MB            │
│                                                │
│  Consider:                                     │
│  • Reducing max encounters (10 → 5)            │
│  • Disabling advanced logging                  │
│  • Freeing up disk space                       │
│                                                │
│  [ Reduce to 5 ]  [ Disable ]  [ Ignore ]      │
└────────────────────────────────────────────────┘
```

---

## 🚀 Migration Path

### Phase 1: Backend Implementation (1 Woche)

```csharp
✅ BattleLog struct
✅ BattleLogRecorder
✅ BattleLogManager (Rolling Window)
✅ BattleLogWriter/Reader (BSON)
✅ AppSettings.EnableAdvancedCombatLogging
✅ Conditional initialization
✅ Runtime toggle support
```

**Deliverable:** Backend fertig, noch kein UI

---

### Phase 2: Settings UI (2-3 Tage)

```
✅ Checkbox "Advanced Combat Logging"
✅ Max Encounters Slider (5, 10, 20, 50)
✅ Current Usage Anzeige
✅ "View Stored Logs" Button
✅ "Clear All" Button
✅ Warning bei Disk-Space
```

**Deliverable:** User kann Feature aktivieren

---

### Phase 3: Logs Window (3-4 Tage)

```
✅ Liste aller gespeicherten Encounters
✅ Sortierung nach Datum
✅ Anzeige: Boss Name, Size, Datum
✅ Actions: View, Export, Delete
✅ Batch-Export (mehrere auswählen)
```

**Deliverable:** User kann Logs verwalten

---

### Phase 4: Replay Window (1-2 Wochen)

```
✅ Timeline mit Events
✅ Play/Pause Controls
✅ Speed Control (0.5x, 1x, 2x, 4x)
✅ Event-Filter (nur Damage, nur Crits, etc.)
✅ Chart-Visualization (Echtzeit)
✅ Export to Video/GIF (optional)
```

**Deliverable:** Vollständige Replay-Funktion

---

## 📈 Performance-Impact

### Standard Mode (Toggle OFF)

```
Keine Änderung zu aktueller Version!

- Keine BattleLog-Structs erstellt
- Keine BSON-Serialization
- Nur aggregierte Stats (wie bisher)

Performance: 0% Overhead
```

---

### Advanced Mode (Toggle ON)

```
Zusätzliche Operationen pro Packet:

1. BattleLog struct erstellen:     ~0.5 µs
2. Zu List hinzufügen:              ~0.1 µs
3. (Beim Save) BSON serialize:      ~50 ms für 8000 Events
4. (Beim Save) File.Write:          ~200 ms für 45 MB

Total pro Packet: ~0.6 µs
Bei 100 Packets/Sekunde: ~0.06 ms = 0.006% CPU-Zeit

Performance-Impact: < 5% (vernachlässigbar)
```

**Fazit:** Selbst im Advanced Mode kaum merkbar!

---

## 🎯 Empfohlene Default-Settings

```csharp
public class AppSettings
{
    // DEFAULT: OFF (casual-friendly)
    public bool EnableAdvancedCombatLogging { get; set; } = false;

    // Wenn aktiviert: 10 Encounters (gutes Mittelmaß)
    public int MaxStoredEncounters { get; set; } = 10;

    // Auto-prompt nach 5 Boss-Kills?
    public bool ShowAdvancedLoggingPrompt { get; set; } = true;
}
```

### Auto-Prompt nach 5 Boss-Kills (Optional)

```
┌────────────────────────────────────────────────┐
│  💡 Tip: Advanced Combat Logging                │
├────────────────────────────────────────────────┤
│                                                │
│  You've completed 5 boss fights!               │
│                                                │
│  Want to enable detailed logging for replay    │
│  and analysis?                                 │
│                                                │
│  • Full combat replay                          │
│  • Export and share logs                       │
│  • Detailed post-fight analysis                │
│                                                │
│  Disk usage: ~450 MB for last 10 encounters    │
│                                                │
│  [ Enable Now ]  [ Maybe Later ]  [ Never ]    │
└────────────────────────────────────────────────┘
```

---

## ✅ Final Summary

### Der Plan

```
┌─────────────────────────────────────────┐
│  BlueMeter Combat Logging               │
├─────────────────────────────────────────┤
│                                         │
│  DEFAULT:                               │
│  ○ Fast Mode (wie bisher)               │
│     - Aggregierte Stats nur             │
│     - SQLite Database                   │
│     - ~5 MB / 10 Sessions               │
│     - Keine Änderung für User           │
│                                         │
│  OPT-IN:                                │
│  ☑ Advanced Mode (neu)                  │
│     - Packet-Level Events               │
│     - BSON Files (.bmlogs)              │
│     - ~450 MB / 10 Encounters           │
│     - Rolling Window (auto-cleanup)     │
│     - Full Replay                       │
│                                         │
└─────────────────────────────────────────┘
```

### Vorteile

| Für... | Vorteil |
|--------|---------|
| **Casual Players** | ✅ Keine Änderung, alles wie bisher |
| **Power Users** | ✅ Volle Features auf Knopfdruck |
| **Entwickler** | ✅ Einfach zu implementieren |
| **Disk Space** | ✅ Kontrolliert (max 10 Encounters) |
| **Performance** | ✅ Minimal (< 5% wenn aktiviert) |

---

**Implementation Effort:** 🔨🔨🔨 (2-3 Wochen komplett)

**Quick Start (nur Backend):** 🔨 (3-5 Tage)

**User Experience:** ⭐⭐⭐⭐⭐
