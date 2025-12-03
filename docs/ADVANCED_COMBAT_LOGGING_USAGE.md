# Advanced Combat Logging - Usage Guide

**Status:** ✅ **Phase 1 COMPLETE** - Backend + Auto-Integration Done!
**Version:** 1.1.0
**Datum:** 2025-12-03

---

## 🎉 Was wurde implementiert?

### ✅ Phase 1: Backend + Auto-Integration (FERTIG!)

1. **BattleLogManager** - Rolling Window mit max 10 Encounters ✅
2. **AppConfig** - Toggle `EnableAdvancedCombatLogging` ✅
3. **DataStorageExtensions Integration** - Automatische BSON-Speicherung ✅
4. **ApplicationStartup** - Auto-Initialisierung aus appsettings.json ✅
5. **Build erfolgreich** - Alle Komponenten kompilieren ✅

### ⏳ Phase 2: UI Integration (TODO)

- Settings UI mit Checkbox (aktuell nur via appsettings.json)
- View Logs Window
- Export/Import Funktionen

---

## 🚀 Wie nutzt man es JETZT?

### Automatische Nutzung (Empfohlen!)

Das Feature ist jetzt vollständig integriert! Du musst nur die Konfiguration aktivieren:

**1. Öffne `appsettings.json` und setze:**

```json
{
    "Config": {
        "EnableAdvancedCombatLogging": true,  // ⚠️ Hier auf true setzen!
        "MaxStoredEncounters": 10,            // Optional: 5, 10, 20, 50
        "BattleLogDirectory": null            // Optional: Custom path oder null
    }
}
```

**2. Starte BlueMeter neu**

Das war's! Ab jetzt:
- ✅ Jeder Boss-Encounter wird automatisch als BSON gespeichert
- ✅ Rolling Window: Max 10 Encounters, älteste werden gelöscht
- ✅ Speicherort: `%LocalAppData%/BlueMeter/CombatLogs/*.bmlogs`
- ✅ Keine Code-Änderungen nötig

**3. Logs ansehen:**

```
📁 %LocalAppData%\BlueMeter\CombatLogs\
   ├── 20251203_103000_abc12345_Boss-Varghedin.bmlogs
   ├── 20251203_104500_def45678_Trash-Mobs.bmlogs
   └── ... (max 10 Dateien)
```

### Manuelle Nutzung (Code-Level)

Falls du das Feature programmatisch nutzen willst (z.B. für Tests):

```csharp
// 1. BattleLogManager erstellen
using BlueMeter.Core.Data;

var manager = new BattleLogManager(
    logDirectory: null,  // null = default (%LocalAppData%/BlueMeter/CombatLogs)
    maxEncounters: 10
);

// 2. BattleLogRecorder nutzen
var recorder = BattleLogRecorder.StartNew();

// ... Kampf läuft ...

// 3. Nach Kampf: Speichern
recorder.Stop();
var logs = recorder.BattleLogs;
var playerInfos = DataStorage.BuildPlayerDicFromBattleLog(logs).Values.ToList();

await manager.SaveEncounterAsync(
    encounterId: "encounter-123",
    bossName: "Varghedin",
    events: logs,
    playerInfos: playerInfos
);

// 4. Automatisches Cleanup (älteste Encounters gelöscht wenn > 10)
```

---

## 📁 File Storage

### Directory Structure

```
%LocalAppData%/BlueMeter/
    └── CombatLogs/
            ├── 20251203_103000_abc12345_Boss-Varghedin.bmlogs
            ├── 20251203_104500_def45678_Trash-Mobs.bmlogs
            ├── 20251203_110000_ghi78901_Boss-Molokhul.bmlogs
            └── ... (max 10 Dateien)
```

### File Format

**BSON** (Binary JSON) - Identisch mit StarResonanceDps `.srlogs` Format:

```json
{
  "FileVersion": "V3_0_0",
  "PlayerInfos": [
    {
      "Uid": 999,
      "Name": "PlayerName",
      "Classes": 1
    }
  ],
  "BattleLogs": [
    {
      "p": 12345,           // PacketID
      "t": 638672140000,    // TimeTicks
      "s": 100001,          // SkillID
      "a": 999,             // AttackerUuid
      "tu": 777,            // TargetUuid
      "v": 12345,           // Value
      "ic": true,           // IsCritical
      "il": false           // IsLucky
      // ... etc
    },
    ...
  ]
}
```

### Filename Schema

```
{Timestamp}_{EncounterId}_{BossName}.bmlogs

Example:
20251203_103000_abc12345_Boss-Varghedin.bmlogs
│           │       │           │        │
│           │       │           │        └─ File Extension (.bmlogs)
│           │       │           └────────── Boss Name (sanitized)
│           │       └────────────────────── Encounter ID (first 8 chars)
│           └────────────────────────────── Time (HHmmss)
└────────────────────────────────────────── Date (yyyyMMdd)
```

---

## 🔧 Configuration

### appsettings.json

```json
{
    "Config": {
        "EnableAdvancedCombatLogging": false,  // Toggle
        "MaxStoredEncounters": 10,             // Rolling window size
        "BattleLogDirectory": null             // null = default
    }
}
```

### AppConfig.cs Properties

```csharp
public class AppConfig : ObservableObject
{
    [ObservableProperty]
    private bool _enableAdvancedCombatLogging = false;

    [ObservableProperty]
    private int _maxStoredEncounters = 10;

    [ObservableProperty]
    private string? _battleLogDirectory = null;
}
```

---

## 🧪 Testing

### Test 1: BattleLogManager Basics

```csharp
// Create manager
var manager = new BattleLogManager(maxEncounters: 3);

// Get stored encounters
var encounters = manager.GetStoredEncounters();
Assert.Empty(encounters);

// Get disk usage
var usage = manager.GetTotalDiskUsageBytes();
Assert.Equal(0, usage);
```

### Test 2: Save & Rolling Window

```csharp
var manager = new BattleLogManager(maxEncounters: 3);

// Save 5 encounters
for (int i = 0; i < 5; i++)
{
    var logs = GenerateTestLogs(1000);  // 1000 events
    var playerInfos = GenerateTestPlayers();

    await manager.SaveEncounterAsync(
        encounterId: $"encounter-{i}",
        bossName: $"Boss-{i}",
        events: logs,
        playerInfos: playerInfos
    );
}

// Should only have 3 (max)
var encounters = manager.GetStoredEncounters();
Assert.Equal(3, encounters.Count);

// Should be newest 3 (encounters 2, 3, 4)
Assert.Contains("encounter-4", encounters[0].FileName);  // Newest
Assert.Contains("encounter-3", encounters[1].FileName);
Assert.Contains("encounter-2", encounters[2].FileName);  // Oldest of remaining
```

### Test 3: Disk Usage Calculation

```csharp
var manager = new BattleLogManager(maxEncounters: 10);

// Save encounter
var logs = GenerateTestLogs(5000);
await manager.SaveEncounterAsync("test", "Boss", logs, players);

// Check size
var usage = manager.GetTotalDiskUsageBytes();
Assert.True(usage > 0);  // Should be ~20-50MB

Console.WriteLine($"Disk usage: {FormatBytes(usage)}");
```

---

## 📊 Disk Space Calculator

| Max Encounters | Avg per Encounter | Total Max Usage |
|---------------|-------------------|-----------------|
| 5 | 45 MB | ~225 MB |
| 10 | 45 MB | ~450 MB |
| 20 | 45 MB | ~900 MB |
| 50 | 45 MB | ~2.25 GB |

**Recommended:** 10 encounters (~450 MB)

---

## 🎯 Next Steps (Phase 2: UI)

### Settings Window UI

```
┌─────────────────────────────────────────────┐
│  ⚙️ Combat Logging Settings                 │
├─────────────────────────────────────────────┤
│                                             │
│  ☐ Enable Advanced Combat Logging          │
│                                             │
│  Max Encounters: [10  ▼]                    │
│  Current Usage: 0/10 (0 MB)                 │
│                                             │
│  [ View Logs ]  [ Clear All ]               │
│                                             │
└─────────────────────────────────────────────┘
```

### View Logs Window

```
┌────────────────────────────────────────────────┐
│  📋 Stored Combat Logs                    [X]  │
├────────────────────────────────────────────────┤
│  Date/Time        Boss         Size   Actions  │
│  ────────────────────────────────────────────  │
│  2025-12-03 10:30 Varghedin   45 MB  [View]💾❌ │
│  2025-12-03 10:45 Trash Mobs  23 MB  [View]💾❌ │
│  ...                                           │
└────────────────────────────────────────────────┘
```

---

## 🐛 Troubleshooting

### Problem: No logs being saved

**Check:**
1. `EnableAdvancedCombatLogging = true` in appsettings.json?
2. BattleLogRecorder started?
3. Check logs directory: `%LocalAppData%/BlueMeter/CombatLogs`

### Problem: Old encounters not deleted

**Check:**
1. `MaxStoredEncounters` setting
2. File permissions in CombatLogs directory
3. Check console output for cleanup errors

### Problem: Files too large

**Solutions:**
1. Reduce `MaxStoredEncounters` (10 → 5)
2. Manual cleanup: Delete .bmlogs files
3. Use `manager.DeleteAllEncountersAsync()`

---

## 📚 API Reference

### BattleLogManager

```csharp
// Constructor
public BattleLogManager(
    string? logDirectory = null,  // null = default
    int maxEncounters = 10
)

// Save encounter (with automatic cleanup)
public async Task SaveEncounterAsync(
    string encounterId,
    string? bossName,
    List<BattleLog> events,
    List<PlayerInfoFileData> playerInfos
)

// Get stored encounters (newest first)
public List<EncounterFileInfo> GetStoredEncounters()

// Get total disk usage
public long GetTotalDiskUsageBytes()

// Delete specific encounter
public async Task DeleteEncounterAsync(string fileName)

// Delete all encounters
public async Task DeleteAllEncountersAsync()
```

### EncounterFileInfo

```csharp
public class EncounterFileInfo
{
    public string FileName { get; init; }
    public string EncounterId { get; init; }
    public string BossName { get; init; }
    public DateTime Timestamp { get; init; }
    public long SizeBytes { get; init; }

    public string FormattedSize { get; }    // "45.23 MB"
    public string FormattedDate { get; }    // "2025-12-03 10:30:00"
}
```

---

## ✅ Summary

**Was funktioniert JETZT:**
- ✅ BattleLogManager erstellen & nutzen
- ✅ Encounters speichern (.bmlogs BSON)
- ✅ Rolling Window (max 10, automatisches Löschen)
- ✅ Disk Usage tracking
- ✅ File listing & deletion
- ✅ **Auto-Integration in DataStorageExtensions** 🆕
- ✅ **Automatische Aktivierung via appsettings.json** 🆕
- ✅ **BattleLogRecorder startet/stoppt automatisch bei Encounters** 🆕

**Was noch fehlt:**
- ⏳ Settings UI (Checkbox) - aktuell nur via appsettings.json
- ⏳ View Logs Window (Liste der gespeicherten Logs)
- ⏳ Replay Window (Visualisierung & Timeline)

**Build Status:** ✅ Kompiliert einwandfrei (0 Errors, 2 Warnings)

---

## 🎯 Integration Details

### Was wurde in DataStorageExtensions integriert?

1. **Initialization** (ApplicationStartup.cs):
   ```csharp
   await DataStorageExtensions.InitializeDatabaseAsync(
       enableAdvancedLogging: configManager.CurrentConfig.EnableAdvancedCombatLogging,
       maxStoredEncounters: configManager.CurrentConfig.MaxStoredEncounters,
       battleLogDirectory: configManager.CurrentConfig.BattleLogDirectory
   );
   ```

2. **Encounter Start** (DataStorageExtensions.StartNewEncounterAsync):
   - Automatisches Starten von BattleLogRecorder
   - Subscribed zu DataStorage.BattleLogCreated event

3. **Encounter End** (DataStorageExtensions.EndCurrentEncounterAsync):
   - Automatisches Stoppen von BattleLogRecorder
   - Abrufen der BattleLogs & PlayerInfos
   - Speichern via BattleLogManager.SaveEncounterAsync()
   - Automatisches Cleanup (Rolling Window)

4. **Public API**:
   ```csharp
   // Zugriff auf BattleLogManager
   var manager = DataStorageExtensions.GetBattleLogManager();

   // Check Status
   bool enabled = DataStorageExtensions.IsAdvancedLoggingEnabled();

   // Liste der gespeicherten Logs
   var encounters = manager?.GetStoredEncounters();

   // Disk Usage
   long bytes = manager?.GetTotalDiskUsageBytes() ?? 0;
   ```

---

**Next:** Phase 2 UI Implementation (Settings Checkbox + View Logs Window)
