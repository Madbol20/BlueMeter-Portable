# BSON Rolling Window Design - Max 10 Encounters

**Konzept:** Automatisches Löschen der ältesten Encounters beim Speichern neuer
**Ziel:** Disk Space begrenzen (z.B. 10 × 50MB = 500MB max)

---

## 📐 Design

### Prinzip: FIFO Queue für Dateien

```
Encounter 1  [oldest]
Encounter 2
Encounter 3
...
Encounter 9
Encounter 10 [newest]

↓ Neuer Encounter 11 wird gespeichert ↓

Encounter 2  [oldest] ← Encounter 1 gelöscht!
Encounter 3
Encounter 4
...
Encounter 10
Encounter 11 [newest]
```

---

## 🗂️ Dateinamen-Schema

### Format

```
{Timestamp}_{EncounterId}_{BossName}.bmlogs
```

**Beispiele:**
```
20251203_103000_abc123_Boss-Varghedin.bmlogs
20251203_104500_def456_Trash-Mobs.bmlogs
20251203_110000_ghi789_Boss-Molokhul.bmlogs
```

**Vorteile:**
- ✅ Chronologisch sortierbar (Timestamp am Anfang)
- ✅ Eindeutig identifizierbar (EncounterId)
- ✅ User-freundlich (BossName sichtbar)

### Timestamp-Format

```csharp
DateTime.Now.ToString("yyyyMMdd_HHmmss")
// Output: "20251203_103000"
```

---

## 💾 Storage-Struktur

### Verzeichnis

```
%LocalAppData%/BlueMeter/CombatLogs/
    ├── 20251203_103000_abc123_Boss-Varghedin.bmlogs
    ├── 20251203_104500_def456_Trash-Mobs.bmlogs
    ├── 20251203_110000_ghi789_Boss-Molokhul.bmlogs
    └── ... (max 10 Dateien)
```

### Metadaten (Optional)

```
%LocalAppData%/BlueMeter/CombatLogs/
    ├── index.json  ← Metadaten über alle Logs
    └── *.bmlogs
```

**index.json:**
```json
{
  "maxEncounters": 10,
  "encounters": [
    {
      "fileName": "20251203_103000_abc123_Boss-Varghedin.bmlogs",
      "encounterId": "abc123",
      "timestamp": "2025-12-03T10:30:00Z",
      "bossName": "Varghedin",
      "players": 4,
      "duration": 300,
      "size": 45234567
    },
    ...
  ]
}
```

**Vorteile:**
- ✅ Schneller Zugriff auf Metadaten (ohne BSON zu parsen)
- ✅ Kann für UI-Liste verwendet werden
- ⚠️ Muss synchron gehalten werden

**Alternativ:** Keine index.json, einfach Dateien scannen (langsamer, aber einfacher)

---

## 🔧 Implementation

### 1. BattleLogManager

```csharp
namespace BlueMeter.Core.Data;

/// <summary>
/// Manages BSON combat log files with automatic cleanup
/// </summary>
public class BattleLogManager
{
    private readonly string _logDirectory;
    private readonly int _maxEncounters;
    private readonly ILogger<BattleLogManager>? _logger;

    public BattleLogManager(
        string? logDirectory = null,
        int maxEncounters = 10,
        ILogger<BattleLogManager>? logger = null)
    {
        _logDirectory = logDirectory ?? GetDefaultLogDirectory();
        _maxEncounters = maxEncounters;
        _logger = logger;

        // Ensure directory exists
        Directory.CreateDirectory(_logDirectory);
    }

    /// <summary>
    /// Get default log directory
    /// </summary>
    private static string GetDefaultLogDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "BlueMeter", "CombatLogs");
    }

    /// <summary>
    /// Save a new encounter and cleanup old ones if needed
    /// </summary>
    public async Task SaveEncounterAsync(
        string encounterId,
        string? bossName,
        List<BattleLog> events)
    {
        // 1. Generate filename
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var sanitizedBossName = SanitizeFileName(bossName ?? "Unknown");
        var fileName = $"{timestamp}_{encounterId}_{sanitizedBossName}.bmlogs";
        var filePath = Path.Combine(_logDirectory, fileName);

        _logger?.LogInformation("Saving encounter {EncounterId} to {FileName}",
            encounterId, fileName);

        // 2. Save BSON file
        await BattleLogWriter.WriteAsync(filePath, events);

        var fileSize = new FileInfo(filePath).Length;
        _logger?.LogInformation("Encounter saved: {Size} bytes ({Events} events)",
            fileSize, events.Count);

        // 3. Cleanup old files
        await CleanupOldEncountersAsync();
    }

    /// <summary>
    /// Remove oldest encounter files if limit exceeded
    /// </summary>
    private async Task CleanupOldEncountersAsync()
    {
        try
        {
            // Get all .bmlogs files sorted by creation time (oldest first)
            var files = Directory.GetFiles(_logDirectory, "*.bmlogs")
                .Select(f => new FileInfo(f))
                .OrderBy(f => f.CreationTimeUtc)  // Oldest first
                .ToList();

            var currentCount = files.Count;

            if (currentCount <= _maxEncounters)
            {
                _logger?.LogDebug("No cleanup needed: {Count}/{Max} encounters",
                    currentCount, _maxEncounters);
                return;
            }

            // Calculate how many to delete
            var toDelete = currentCount - _maxEncounters;
            _logger?.LogInformation("Cleaning up {Count} old encounters ({Current}/{Max})",
                toDelete, currentCount, _maxEncounters);

            // Delete oldest files
            for (int i = 0; i < toDelete; i++)
            {
                var file = files[i];
                _logger?.LogInformation("Deleting old encounter: {FileName} ({Size} bytes)",
                    file.Name, file.Length);

                await Task.Run(() => file.Delete());
            }

            _logger?.LogInformation("Cleanup completed: {Remaining} encounters remaining",
                currentCount - toDelete);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during cleanup");
            // Don't throw - cleanup failure shouldn't break saving
        }
    }

    /// <summary>
    /// Get list of all stored encounters
    /// </summary>
    public List<EncounterFileInfo> GetStoredEncounters()
    {
        var files = Directory.GetFiles(_logDirectory, "*.bmlogs")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)  // Newest first for display
            .Select(f => ParseFileName(f))
            .Where(info => info != null)
            .Cast<EncounterFileInfo>()
            .ToList();

        _logger?.LogDebug("Found {Count} stored encounters", files.Count);
        return files;
    }

    /// <summary>
    /// Load encounter from file
    /// </summary>
    public async Task<List<BattleLog>> LoadEncounterAsync(string fileName)
    {
        var filePath = Path.Combine(_logDirectory, fileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Encounter file not found: {fileName}");

        _logger?.LogInformation("Loading encounter from {FileName}", fileName);
        return await BattleLogReader.ReadAsync(filePath);
    }

    /// <summary>
    /// Delete specific encounter
    /// </summary>
    public async Task DeleteEncounterAsync(string fileName)
    {
        var filePath = Path.Combine(_logDirectory, fileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Encounter file not found: {fileName}");

        _logger?.LogInformation("Deleting encounter {FileName}", fileName);
        await Task.Run(() => File.Delete(filePath));
    }

    /// <summary>
    /// Get total disk usage
    /// </summary>
    public long GetTotalDiskUsageBytes()
    {
        return Directory.GetFiles(_logDirectory, "*.bmlogs")
            .Select(f => new FileInfo(f).Length)
            .Sum();
    }

    /// <summary>
    /// Parse filename into structured info
    /// </summary>
    private EncounterFileInfo? ParseFileName(FileInfo file)
    {
        try
        {
            // Expected format: 20251203_103000_abc123_Boss-Name.bmlogs
            var nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
            var parts = nameWithoutExt.Split('_', 4);

            if (parts.Length < 3)
            {
                _logger?.LogWarning("Invalid filename format: {FileName}", file.Name);
                return null;
            }

            var dateStr = parts[0];
            var timeStr = parts[1];
            var encounterId = parts[2];
            var bossName = parts.Length > 3 ? parts[3] : "Unknown";

            // Parse timestamp
            var timestampStr = $"{dateStr}_{timeStr}";
            if (!DateTime.TryParseExact(timestampStr, "yyyyMMdd_HHmmss", null,
                System.Globalization.DateTimeStyles.None, out var timestamp))
            {
                _logger?.LogWarning("Could not parse timestamp: {Timestamp}", timestampStr);
                timestamp = file.CreationTimeUtc;
            }

            return new EncounterFileInfo
            {
                FileName = file.Name,
                EncounterId = encounterId,
                BossName = bossName,
                Timestamp = timestamp,
                SizeBytes = file.Length,
                CreatedUtc = file.CreationTimeUtc
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error parsing filename: {FileName}", file.Name);
            return null;
        }
    }

    /// <summary>
    /// Sanitize boss name for filename
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        // Remove invalid filename characters
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("", name.Split(invalid));

        // Replace spaces with hyphens
        sanitized = sanitized.Replace(' ', '-');

        // Limit length
        if (sanitized.Length > 50)
            sanitized = sanitized.Substring(0, 50);

        return sanitized;
    }
}

/// <summary>
/// Information about a stored encounter file
/// </summary>
public class EncounterFileInfo
{
    public string FileName { get; init; } = string.Empty;
    public string EncounterId { get; init; } = string.Empty;
    public string BossName { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public long SizeBytes { get; init; }
    public DateTime CreatedUtc { get; init; }

    public string FormattedSize => FormatBytes(SizeBytes);
    public string FormattedDate => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
```

---

### 2. Integration in DataStorageExtensions

```csharp
// BlueMeter.Core/Data/DataStorageExtensions.cs

public static class DataStorageExtensions
{
    private static BattleLogManager? _battleLogManager;
    private static BattleLogRecorder? _battleLogRecorder;

    public static async Task InitializeDatabaseAsync(
        IDataStorage? dataStorage = null,
        string? databasePath = null,
        object? chartDataService = null,
        bool enableBattleLogging = false,  // ← NEW
        int maxStoredEncounters = 10,      // ← NEW
        ...)
    {
        // ... existing initialization

        // Initialize battle log manager
        if (enableBattleLogging)
        {
            _battleLogManager = new BattleLogManager(
                maxEncounters: maxStoredEncounters,
                logger: null  // TODO: Add logger
            );

            _battleLogRecorder = new BattleLogRecorder(_dataStorage);
            _battleLogRecorder.Start();

            Console.WriteLine($"[DataStorageExtensions] Battle logging enabled (max {maxStoredEncounters} encounters)");
        }
    }

    private static async void OnNewSectionCreated()
    {
        try
        {
            // ... existing code (save to SQLite)

            // NEW: Save BSON battle log if enabled
            if (_battleLogRecorder != null && _battleLogManager != null)
            {
                await SaveBattleLogAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnNewSectionCreated: {ex.Message}");
        }
    }

    private static async Task SaveBattleLogAsync()
    {
        try
        {
            if (_battleLogRecorder == null || _battleLogManager == null)
                return;

            var logs = _battleLogRecorder.GetLogsAndReset();

            if (logs.Count == 0)
            {
                Console.WriteLine("[DataStorageExtensions] No battle logs to save");
                return;
            }

            // Get encounter info
            var encounterId = _encounterService?.CurrentEncounterId ?? Guid.NewGuid().ToString();
            var bossName = _encounterService?.CurrentBossName ?? "Unknown";

            Console.WriteLine($"[DataStorageExtensions] Saving {logs.Count} battle events for encounter {encounterId}");

            await _battleLogManager.SaveEncounterAsync(encounterId, bossName, logs);

            var usage = _battleLogManager.GetTotalDiskUsageBytes();
            var encounters = _battleLogManager.GetStoredEncounters();

            Console.WriteLine($"[DataStorageExtensions] Battle log saved: {encounters.Count} encounters, {FormatBytes(usage)} total");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DataStorageExtensions] Error saving battle log: {ex.Message}");
        }
    }
}
```

---

### 3. BattleLogRecorder mit GetLogsAndReset

```csharp
public class BattleLogRecorder
{
    private readonly List<BattleLog> _logs = new();
    private readonly object _lock = new();

    // ... existing code

    /// <summary>
    /// Get all logs and clear the buffer (for saving)
    /// </summary>
    public List<BattleLog> GetLogsAndReset()
    {
        lock (_lock)
        {
            var copy = new List<BattleLog>(_logs);
            _logs.Clear();
            return copy;
        }
    }
}
```

---

### 4. Settings

```csharp
// BlueMeter.Core/Configuration/AppSettings.cs

public class AppSettings
{
    /// <summary>
    /// Enable packet-level battle logging
    /// </summary>
    public bool EnableBattleLogging { get; set; } = false;

    /// <summary>
    /// Maximum number of encounters to store (oldest deleted first)
    /// </summary>
    public int MaxStoredEncounters { get; set; } = 10;

    /// <summary>
    /// Custom directory for battle logs (null = default)
    /// </summary>
    public string? BattleLogDirectory { get; set; } = null;
}
```

---

## 📊 Disk Space Kalkulation

### Beispiel-Szenario

**Annahmen:**
- 5-Minuten Boss-Fight
- 4 Spieler
- ~8000 Events
- ~45 MB pro Encounter (BSON)

### Bei Max 10 Encounters

```
Total Disk Usage = 10 × 45 MB = 450 MB
```

### Bei Max 20 Encounters

```
Total Disk Usage = 20 × 45 MB = 900 MB
```

### Konfigurierbar

User kann wählen (Settings):
- 5 Encounters → ~225 MB
- 10 Encounters → ~450 MB (Standard)
- 20 Encounters → ~900 MB
- 50 Encounters → ~2.25 GB

---

## 🎨 UI Integration

### Settings Window

```
┌────────────────────────────────────────────┐
│  ⚙️ Combat Logging Settings                │
├────────────────────────────────────────────┤
│                                            │
│  ☑ Enable Detailed Combat Logging         │
│                                            │
│  Max Stored Encounters: [10      ] ▼      │
│  (Oldest encounters auto-deleted)          │
│                                            │
│  Current Usage: 3/10 encounters (135 MB)   │
│                                            │
│  Log Directory:                            │
│  C:\Users\...\BlueMeter\CombatLogs  [...]  │
│                                            │
│  [ View Logs ]  [ Clear All ]              │
│                                            │
└────────────────────────────────────────────┘
```

### Combat Logs Window (NEW)

```
┌────────────────────────────────────────────────────────────┐
│  📋 Stored Combat Logs                              [X]    │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌────────────────────────────────────────────────────┐   │
│  │ Date/Time        Boss Name       Size    Actions  │   │
│  ├────────────────────────────────────────────────────┤   │
│  │ 2025-12-03 10:30 Varghedin       45 MB   [View]💾❌│   │
│  │ 2025-12-03 10:45 Trash Mobs      23 MB   [View]💾❌│   │
│  │ 2025-12-03 11:00 Molokhul        52 MB   [View]💾❌│   │
│  │ ...                                                │   │
│  └────────────────────────────────────────────────────┘   │
│                                                            │
│  Total: 3/10 encounters (120 MB / ~450 MB max)             │
│                                                            │
│  [ Export Selected ] [ Delete Selected ] [ Clear All ]     │
│                                                            │
└────────────────────────────────────────────────────────────┘

Legend:
  [View]  - Open in Replay Window
  💾      - Export to custom location
  ❌      - Delete this encounter
```

---

## 🚀 Vorteile dieses Ansatzes

### 1. ✅ Kontrollierter Disk Space

- Maximal 10 Encounters = **vorhersehbar**
- Kein unendliches Wachstum
- User kann Limit selbst konfigurieren

### 2. ✅ Einfach zu implementieren

- Keine komplexe Logik
- Standard File I/O
- Sortierung nach Timestamp

### 3. ✅ User-freundlich

- Immer die letzten 10 Kämpfe verfügbar
- Wichtige Kämpfe können manuell exportiert werden
- "Set & Forget" - keine Wartung nötig

### 4. ✅ Flexibel

- Limit konfigurierbar (5, 10, 20, 50, ...)
- Kann später erweitert werden (z.B. "Favorite" markieren)
- Kompatibel mit Export/Import

---

## 📈 Performance

### Cleanup Performance

```csharp
// Worst Case: 100 Encounters zu 50MB
// Sortieren: O(n log n) = O(100 log 100) ≈ 200 Operationen
// Löschen: 90 × File.Delete() ≈ 90ms

// Typischer Fall: 11 Encounters
// Sortieren: O(11 log 11) ≈ 11 Operationen
// Löschen: 1 × File.Delete() ≈ 1ms

Total: < 10ms (vernachlässigbar)
```

### Datei-Scan Performance

```csharp
// Directory.GetFiles("*.bmlogs") für 10 Dateien
// Sortieren nach CreationTime
// → < 5ms

// Mit 100 Dateien
// → < 20ms

Fazit: Schnell genug, kein Caching nötig
```

---

## 🔒 Edge Cases

### 1. Disk Voll

```csharp
try
{
    await BattleLogWriter.WriteAsync(filePath, events);
}
catch (IOException ex) when (ex.Message.Contains("disk full"))
{
    _logger?.LogError("Disk full - attempting emergency cleanup");

    // Emergency: Delete oldest 5 encounters
    await EmergencyCleanupAsync(deleteCount: 5);

    // Retry once
    await BattleLogWriter.WriteAsync(filePath, events);
}
```

### 2. Korrupte Dateien

```csharp
private EncounterFileInfo? ParseFileName(FileInfo file)
{
    try
    {
        // ... parse logic
    }
    catch
    {
        // Ignoriere korrupte/ungültige Dateien
        _logger?.LogWarning("Skipping invalid file: {FileName}", file.Name);
        return null;
    }
}
```

### 3. Gleichzeitiger Zugriff

```csharp
// File.Delete ist thread-safe
// Falls Datei gerade gelesen wird → IOException
// → Retry nach kurzer Pause

private async Task DeleteFileWithRetry(string filePath, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            File.Delete(filePath);
            return;
        }
        catch (IOException) when (i < maxRetries - 1)
        {
            await Task.Delay(100); // 100ms Pause
        }
    }
}
```

---

## 🎯 Erweiterungen (Zukunft)

### 1. "Favorite" Markierung

```csharp
public class EncounterFileInfo
{
    public bool IsFavorite { get; set; }  // ← Nie löschen
}

// Beim Cleanup
var nonFavorites = files.Where(f => !f.IsFavorite);
```

### 2. Kategorien/Tags

```
Boss-Fights/
  └── 20251203_103000_abc123_Boss-Varghedin.bmlogs
Dungeons/
  └── 20251203_110000_def456_Dungeon-Run.bmlogs
Practice/
  └── 20251203_120000_ghi789_Training.bmlogs
```

### 3. Kompression

```csharp
// BSON + GZip
await using var fileStream = File.Create(filePath);
await using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
await BattleLogWriter.WriteAsync(gzipStream, events);

// Reduzierung: ~50% (45MB → 22MB)
```

---

## ✅ Zusammenfassung

**Rolling Window mit Max 10 Encounters:**

| Aspekt | Beschreibung |
|--------|--------------|
| **Max Encounters** | 10 (konfigurierbar) |
| **Max Disk Space** | ~450 MB @ 10 Encounters |
| **Cleanup** | Automatisch beim Speichern |
| **Strategie** | FIFO (Oldest deleted first) |
| **Format** | BSON (.bmlogs) |
| **Dateinamen** | `{Timestamp}_{EncounterId}_{BossName}.bmlogs` |

**Implementation Effort:** 🔨🔨 (2-3 Tage)

**User Experience:** ⭐⭐⭐⭐⭐
- Set & Forget
- Immer die letzten Kämpfe
- Kein Disk-Space-Problem
