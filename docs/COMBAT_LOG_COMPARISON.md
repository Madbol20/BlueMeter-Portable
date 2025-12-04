# Combat Log Storage: BlueMeter vs StarResonanceDps

**Datum:** 2025-12-03
**Status:** ⚠️ **KRITISCHE LÜCKEN IN BLUEMETER IDENTIFIZIERT**

---

## 🔍 Executive Summary

Nach detaillierter Analyse beider Projekte wurden **fundamentale Unterschiede** in der Combat-Log-Speicherung gefunden:

| Aspekt | StarResonanceDps | BlueMeter |
|--------|------------------|-----------|
| **Granularität** | ✅ **Packet-Level** (jedes Event) | ❌ **Aggregiert** (nur Summen) |
| **Replay-Fähigkeit** | ✅ **JA** - vollständig | ❌ **NEIN** - unmöglich |
| **Detailanalyse** | ✅ **JA** - jeder Hit | ⚠️ **BEGRENZT** - nur Totals |
| **Export** | ✅ **BSON-Dateien** | ⚠️ **SQLite** (nur Stats) |
| **Disk Usage** | ⚠️ **HOCH** (100MB+/Session) | ✅ **NIEDRIG** (1-5MB/Session) |

**Fazit:** BlueMeter speichert **NUR** aggregierte Statistiken. StarResonanceDps speichert **JEDES** Combat-Event.

---

## 📊 Detaillierter Vergleich

### 1. StarResonanceDps: Packet-Level Recording

#### Architektur

```
PacketAnalyzer → BattleLog (struct) → BattleLogQueue → BattleLogRecorder
                                                            ↓
                                                      BSON File (.srlogs)
```

#### Was wird gespeichert?

**Jeder einzelne Damage/Heal-Event als `BattleLog`:**

```csharp
public struct BattleLog
{
    public long PacketID;          // Eindeutige Packet-ID
    public long TimeTicks;         // Exakte Timestamp
    public long SkillID;           // Welche Skill
    public long AttackerUuid;      // Wer hat angegriffen
    public long TargetUuid;        // Wen hat es getroffen
    public long Value;             // Wieviel Schaden/Heal
    public int ValueElementType;   // Element (Fire, Ice, etc.)
    public int DamageSourceType;   // Quelle (Direct, DoT, etc.)
    public bool IsAttackerPlayer;  // Spieler oder NPC?
    public bool IsTargetPlayer;    // Spieler oder NPC?
    public bool IsLucky;           // Lucky Hit?
    public bool IsCritical;        // Critical Hit?
}
```

**Beispiel:** Ein 5-Minuten-Kampf mit 3 Spielern:
- ~5000-10000 Events gespeichert
- Jeder Hit, jede Heilung, jeder Tick
- Komplette Timeline rekonstruierbar

#### Storage Format

**BSON (Binary JSON) Dateien:**
- Dateiname: `PlayerName_2025_12_03_10_30_00_2025_12_03_10_35_00.srlogs`
- Komprimiert mit verkürzten Property-Namen ("p", "t", "s", "a", etc.)
- ~10-100MB pro Session

```json
{
  "FileVersion": "V3_0_0",
  "PlayerInfos": [ ... ],
  "BattleLogs": [
    { "p": 12345, "t": 638672140000000000, "s": 100001, "a": 999, "tu": 777, "v": 12345, ... },
    { "p": 12346, "t": 638672140200000000, "s": 100002, "a": 999, "tu": 777, "v": 8765, ... },
    ...
  ]
}
```

#### Vorteile

✅ **Vollständige Replay-Fähigkeit:**
- Kann den kompletten Kampf Packet-für-Packet abspielen
- Exakte Timeline-Analyse
- Kann nachträglich neue Metriken berechnen

✅ **Detaillierte Analyse:**
- Einzelne Skill-Casts analysieren
- Timing zwischen Events
- Buff/Debuff-Uptime (falls implementiert)

✅ **Export & Sharing:**
- Kann Logs teilen
- Andere Tools können Logs importieren
- Archivierung möglich

#### Nachteile

⚠️ **Hoher Disk Space:**
- 10-100MB pro 5-Minuten-Kampf
- Mehrere GB nach einigen Stunden

⚠️ **Performance:**
- BSON Serialization/Deserialization
- Große Dateien laden langsam

---

### 2. BlueMeter: Aggregated Statistics Only

#### Architektur

```
PacketAnalyzer → DpsData (aggregiert) → EncounterRepository → SQLite Database
                                                                    ↓
                                                              PlayerEncounterStats
```

#### Was wird gespeichert?

**NUR aggregierte Summen pro Encounter:**

```csharp
public class PlayerEncounterStatsEntity
{
    // AGGREGATE TOTALS
    public long TotalAttackDamage;    // Summe aller Schäden
    public long TotalTakenDamage;     // Summe aller genommenen Schäden
    public long TotalHeal;            // Summe aller Heals

    // TIMING
    public long StartLoggedTick;      // Start-Zeit
    public long LastLoggedTick;       // End-Zeit

    // CALCULATED STATS
    public double DPS;                // Durchschnittlicher DPS
    public double HPS;                // Durchschnittlicher HPS
    public int TotalHits;             // Anzahl Hits
    public int TotalCrits;            // Anzahl Crits
    public double CritRate;           // Crit Rate %

    // SKILL BREAKDOWN (aggregiert)
    public string SkillDataJson;      // Dictionary<SkillId, AggregatedStats>

    // CHART HISTORY (neu)
    public string DpsHistoryJson;     // List<ChartDataPoint> (500 Punkte max)
    public string HpsHistoryJson;     // List<ChartDataPoint> (500 Punkte max)
}
```

**Beispiel:** Derselbe 5-Minuten-Kampf:
- 1 Encounter-Eintrag
- 3-8 PlayerStats-Einträge
- Skills als JSON (aggregiert)
- Chart-History (500 Punkte à 5 Samples/Sekunde = ~100 Sekunden)

#### Storage Format

**SQLite Database:**
- `BlueMeter.db` (~1-5MB pro Session)
- Relational Schema:
  - `Encounters` Table
  - `PlayerEncounterStats` Table
  - `Players` Table (Cache)

**Kein Zugriff auf:**
- ❌ Einzelne Packets/Events
- ❌ Exakte Timestamps pro Hit
- ❌ Buff/Debuff-Timings
- ❌ Skill-Rotation-Details

#### Vorteile

✅ **Niedriger Disk Space:**
- 1-5MB pro Session
- Kann Tausende Encounters speichern

✅ **Schnelle Queries:**
- SQLite sehr schnell für Aggregat-Abfragen
- EF Core Integration

✅ **Strukturiert:**
- Relational Schema
- Foreign Keys, Indices
- Einfache Datenanalyse

#### Nachteile

❌ **KEINE Replay-Fähigkeit:**
- Kann Kampf NICHT rekonstruieren
- Nur Summen verfügbar

❌ **Begrenzte Post-Analysis:**
- Neue Metriken NICHT nachträglich berechenbar
- Chart-History auf 500 Punkte begrenzt

❌ **Kein Export:**
- Schwer zu teilen
- Keine Standard-Formate

---

## 🚨 Kritische Lücken in BlueMeter

### 1. ❌ Keine Event-Level-Daten

**Problem:**
BlueMeter speichert **NUR** aggregierte Summen. Es ist unmöglich:
- Kampf abzuspielen
- Einzelne Skill-Casts zu analysieren
- Buff-Uptimes zu berechnen
- Rotation-Analyse zu machen

**Beispiel:**
```
StarResonanceDps kann sagen:
  "Um 10:32:15.123 hat Spieler A Skill 'Fireball' benutzt,
   der 12345 Schaden an Boss B gemacht hat (Crit)"

BlueMeter kann nur sagen:
  "Spieler A hat insgesamt 1.2M Schaden gemacht"
```

### 2. ⚠️ Limitierte Chart-History

**Problem:**
Chart-History ist auf **500 Datenpunkte** begrenzt:
- @ 5 Samples/Sekunde = 100 Sekunden = 1:40 Minuten
- @ 1 Sample/Sekunde = 500 Sekunden = 8:20 Minuten

Für längere Kämpfe (10+ Minuten) fehlen Details!

### 3. ❌ Kein Export/Import

**Problem:**
- Logs können NICHT exportiert werden
- Logs können NICHT mit anderen geteilt werden
- Keine Backup-Strategie außer DB-Copy

### 4. ❌ Keine Nachträgliche Analyse

**Problem:**
Wenn später eine neue Metrik benötigt wird (z.B. "Average Hit Size"), **UNMÖGLICH** zu berechnen, weil einzelne Hits nicht gespeichert sind.

---

## 💡 Verbesserungsvorschläge für BlueMeter

### Option A: **Hybrid-Ansatz** (EMPFOHLEN)

Kombiniere beide Ansätze:

1. **Behalte** SQLite für aggregierte Stats (schneller Zugriff)
2. **FÜGE HINZU** optionales Packet-Level-Logging

```
┌─────────────────────────────────────────┐
│        PacketAnalyzer                   │
└─────────────┬───────────────────────────┘
              │
              ├──→ DpsData (Aggregation)
              │       ↓
              │   SQLite (Stats)
              │
              └──→ BattleLogRecorder (Optional)
                      ↓
                  BSON Files (.srlogs)
```

**Vorteile:**
- ✅ Standard-Nutzung: Schnell, wenig Disk Space
- ✅ Power-User: Können Packet-Logging aktivieren
- ✅ Replay nur wenn benötigt

**Implementation:**
- Füge `BattleLogRecorder` aus StarResonanceDps hinzu
- User-Setting: "Enable Detailed Logging"
- Export-Button: "Export to .bmlogs"

---

### Option B: **Erweiterte SQLite-Tabelle**

Füge eine neue Tabelle hinzu:

```sql
CREATE TABLE BattleEvents (
    Id INTEGER PRIMARY KEY,
    EncounterId INTEGER NOT NULL,
    TimeTicks INTEGER NOT NULL,
    SkillId INTEGER NOT NULL,
    AttackerUid INTEGER NOT NULL,
    TargetUid INTEGER NOT NULL,
    Value INTEGER NOT NULL,
    EventType INTEGER NOT NULL,  -- Damage, Heal, Buff, etc.
    Flags INTEGER NOT NULL,      -- IsCrit, IsLucky, etc.
    FOREIGN KEY (EncounterId) REFERENCES Encounters(Id)
);

CREATE INDEX idx_battle_events_encounter ON BattleEvents(EncounterId);
CREATE INDEX idx_battle_events_time ON BattleEvents(TimeTicks);
```

**Vorteile:**
- ✅ Alles in einer Datenbank
- ✅ Relational Queries möglich
- ✅ Kein neues File-Format

**Nachteile:**
- ⚠️ DB kann sehr groß werden (100MB+ pro Session)
- ⚠️ Langsamer als BSON
- ⚠️ SQLite nicht optimal für Time-Series

---

### Option C: **Separate Time-Series-DB**

Verwende eine dedizierte Time-Series-Datenbank:

```
- InfluxDB (embedded)
- SQLite mit Time-Series Extension
- Custom Binary Format
```

**Vorteile:**
- ✅ Optimal für Zeit-basierte Daten
- ✅ Effiziente Kompression
- ✅ Schnelle Queries

**Nachteile:**
- ⚠️ Komplexer Setup
- ⚠️ Zusätzliche Dependency

---

### Option D: **Chunked Storage**

Kombiniere BSON + SQLite:

```
SQLite (Metadaten + Stats)
    ↓
    Links to → BSON Chunks (Detailed Events)
                  ↓
               Optional, Auto-Delete after 7 days
```

**Vorteile:**
- ✅ Beste aus beiden Welten
- ✅ Auto-Cleanup alter Logs
- ✅ User kann wählen

---

## 📋 Implementation Roadmap

### Phase 1: **Battle Event Capture** (2-3 Tage)

**Ziel:** Packet-Level-Events erfassen (noch nicht speichern)

1. Port `BattleLog` struct von StarResonanceDps
2. Erstelle `BattleLogQueue` für Buffering
3. Hook PacketAnalyzer Events
4. Event-Handler für Damage/Heal/Buff
5. Test: Events in Memory sammeln

**Deliverable:** Events werden gesammelt, aber noch nicht gespeichert

---

### Phase 2: **Storage Layer** (3-4 Tage)

**Ziel:** Events in Dateien speichern

**Option A: BSON Files (wie StarResonanceDps)**
1. Port `BattleLogWriter`/`BattleLogReader`
2. File-Format definieren (`.bmlogs`)
3. Auto-Naming: `PlayerName_DateTime.bmlogs`
4. Kompression aktivieren (BSON + GZip)

**Option B: SQLite Events Table**
1. Erstelle `BattleEvents` Table
2. Batch-Insert (100 Events auf einmal)
3. Indices für Performance
4. Auto-Vacuum konfigurieren

**Deliverable:** Events werden persistent gespeichert

---

### Phase 3: **UI Integration** (2-3 Tage)

**Ziel:** User kann Logging steuern

1. Settings UI:
   - ☑ "Enable Detailed Combat Logging"
   - 🔘 Storage Format (BSON / SQLite)
   - 🔢 Max Storage Size (MB)
   - 🔢 Auto-Delete after X days

2. Export UI:
   - Button: "Export Current Encounter"
   - Format wählen: `.bmlogs` / `.json` / `.csv`
   - Save-Dialog

3. Import UI:
   - Button: "Import Combat Log"
   - File-Picker
   - Load & Analyze

**Deliverable:** User kann Logs exportieren/importieren

---

### Phase 4: **Replay & Analysis** (3-5 Tage)

**Ziel:** Detaillierte Post-Fight-Analyse

1. **Replay Window:**
   - Timeline-Slider
   - Play/Pause Controls
   - Event-List (filterable)
   - Damage-Zahlen animiert

2. **Advanced Charts:**
   - DPS Timeline (echte Packets, nicht 500-Punkt-Limit)
   - Skill-Usage Timeline
   - Buff-Uptime-Timeline
   - Damage-Breakdown per Target

3. **Export Reports:**
   - HTML Report mit Charts
   - WarcraftLogs-Style Analyse
   - Share-Link (optional Cloud-Upload)

**Deliverable:** Vollständige Replay-Funktionalität

---

### Phase 5: **Performance & Optimization** (2-3 Tage)

1. Batch-Processing optimieren
2. Memory-Leaks fixen
3. Async I/O für File-Writes
4. Kompression testen (verschiedene Levels)
5. Benchmark: 1000 Events/Sekunde

**Deliverable:** Stabile, performante Lösung

---

## 🎯 Prioritäten-Matrix

| Feature | Impact | Effort | Priority |
|---------|--------|--------|----------|
| **Packet-Level-Capture** | ⭐⭐⭐⭐⭐ | 🔨🔨 | 🟢 **HIGH** |
| **BSON Export** | ⭐⭐⭐⭐ | 🔨🔨 | 🟢 **HIGH** |
| **User Settings UI** | ⭐⭐⭐ | 🔨 | 🟡 MEDIUM |
| **Import Funktion** | ⭐⭐⭐ | 🔨🔨 | 🟡 MEDIUM |
| **Replay Window** | ⭐⭐⭐⭐⭐ | 🔨🔨🔨🔨 | 🟠 NICE-TO-HAVE |
| **HTML Reports** | ⭐⭐ | 🔨🔨 | 🔴 LOW |

**Empfohlener Start:** Phase 1 + 2 (Packet-Level-Capture + BSON Export)

---

## 🛠️ Code-Beispiele

### 1. BattleLog Structure (Port von StarResonanceDps)

```csharp
// BlueMeter.Core/Data/Models/BattleLog.cs
namespace BlueMeter.Core.Data.Models;

/// <summary>
/// Single combat event (damage, heal, buff, etc.)
/// Struct for memory efficiency
/// </summary>
public readonly struct BattleLog
{
    public long PacketID { get; init; }
    public long TimeTicks { get; init; }
    public long SkillID { get; init; }
    public long AttackerUuid { get; init; }
    public long TargetUuid { get; init; }
    public long Value { get; init; }
    public BattleLogType Type { get; init; }  // Damage, Heal, Buff, etc.
    public byte Flags { get; init; }          // IsCrit, IsLucky, etc. (bitfield)

    public bool IsCritical => (Flags & 0x01) != 0;
    public bool IsLucky => (Flags & 0x02) != 0;
    public bool IsAttackerPlayer => (Flags & 0x04) != 0;
    public bool IsTargetPlayer => (Flags & 0x08) != 0;
}

public enum BattleLogType : byte
{
    Damage = 0,
    Heal = 1,
    Buff = 2,
    Debuff = 3,
    Death = 4
}
```

### 2. BattleLogRecorder (Simplified)

```csharp
// BlueMeter.Core/Data/BattleLogRecorder.cs
namespace BlueMeter.Core.Data;

public sealed class BattleLogRecorder : IDisposable
{
    private readonly List<BattleLog> _logs = new();
    private readonly object _lock = new();
    public bool IsRecording { get; private set; }

    public void Start()
    {
        lock (_lock)
        {
            if (IsRecording) throw new InvalidOperationException("Already recording");
            IsRecording = true;
            DataStorage.DamageEvent += OnDamageEvent;  // Hook events
            DataStorage.HealEvent += OnHealEvent;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!IsRecording) return;
            IsRecording = false;
            DataStorage.DamageEvent -= OnDamageEvent;
            DataStorage.HealEvent -= OnHealEvent;
        }
    }

    private void OnDamageEvent(DamagePacket packet)
    {
        lock (_lock)
        {
            var log = new BattleLog
            {
                PacketID = packet.PacketID,
                TimeTicks = DateTime.UtcNow.Ticks,
                SkillID = packet.SkillID,
                AttackerUuid = packet.AttackerUuid,
                TargetUuid = packet.TargetUuid,
                Value = packet.Damage,
                Type = BattleLogType.Damage,
                Flags = BuildFlags(packet)
            };
            _logs.Add(log);
        }
    }

    public async Task ExportAsync(string filePath)
    {
        lock (_lock)
        {
            if (IsRecording) throw new InvalidOperationException("Stop recording first");
        }

        await BattleLogWriter.WriteAsync(filePath, _logs);
    }

    public void Dispose()
    {
        Stop();
        _logs.Clear();
    }
}
```

### 3. Settings Integration

```csharp
// BlueMeter.Core/Configuration/AppSettings.cs
public class AppSettings
{
    // ... existing settings

    /// <summary>
    /// Enable detailed packet-level combat logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Storage format for detailed logs
    /// </summary>
    public LogStorageFormat LogFormat { get; set; } = LogStorageFormat.BSON;

    /// <summary>
    /// Maximum storage size for logs in MB (0 = unlimited)
    /// </summary>
    public int MaxLogStorageMB { get; set; } = 500;

    /// <summary>
    /// Auto-delete logs older than X days (0 = never)
    /// </summary>
    public int AutoDeleteLogsDays { get; set; } = 7;
}

public enum LogStorageFormat
{
    None = 0,
    BSON = 1,
    SQLite = 2,
    JSON = 3
}
```

---

## 📈 Storage-Vergleich

### Beispiel-Szenario: 5-Minuten Boss-Fight, 4 Spieler

| Daten | StarResonanceDps (BSON) | BlueMeter (Current) | BlueMeter (mit Events) |
|-------|-------------------------|---------------------|------------------------|
| **Events** | 8000 | 0 | 8000 |
| **File Size** | 45 MB | 500 KB | 45 MB (BSON) / 80 MB (SQLite) |
| **Replay** | ✅ JA | ❌ NEIN | ✅ JA |
| **Query Speed** | ⚠️ Slow (parse BSON) | ✅ Fast (indexed) | 🟡 Medium |
| **Disk/Session** | 45 MB | 500 KB | 45 MB |
| **Disk/10 Hours** | 5.4 GB | 60 MB | 5.4 GB |

**Empfehlung:**
- Standardmäßig: **NUR** aggregierte Stats (wie bisher)
- Optional aktivierbar: **Detailed Logging** für wichtige Fights
- Auto-Delete nach 7 Tagen (konfigurierbar)

---

## 🔥 Quick Win: Minimal Viable Product

**Ziel:** Innerhalb 1-2 Tagen implementierbar

### Was machen?

1. ✅ Port `BattleLog` struct
2. ✅ Erstelle `BattleLogRecorder` (simplified)
3. ✅ Hook Damage/Heal Events
4. ✅ Export zu JSON (nicht BSON, zu komplex)
5. ✅ UI: "Export Current Fight" Button

### Was NICHT machen?

- ❌ Keine automatische Recording
- ❌ Keine Settings
- ❌ Keine Import-Funktion
- ❌ Kein Replay-UI
- ❌ Keine SQLite-Integration

### Result

User kann **manuell** einen Kampf aufzeichnen und als JSON exportieren.

**Beispiel JSON:**
```json
{
  "encounter": {
    "id": "abc-123",
    "start": "2025-12-03T10:30:00Z",
    "duration": 300000
  },
  "players": [ ... ],
  "events": [
    {
      "t": 638672140000000000,
      "skill": 100001,
      "from": 999,
      "to": 777,
      "dmg": 12345,
      "crit": true
    },
    ...
  ]
}
```

---

## 🎬 Conclusion

**BlueMeter braucht dringend:**

1. 🔴 **Packet-Level-Logging** (optional)
2. 🟠 **Export/Import-Funktionalität**
3. 🟡 **Erweiterte Chart-History** (> 500 Punkte)
4. 🟢 **Replay-UI** (nice-to-have)

**Empfehlung:**
- Start mit **Hybrid-Ansatz** (Option A)
- Phase 1 + 2 sofort starten (1-2 Wochen)
- User-Testing nach Phase 3
- Replay-UI als Feature für v2.0

**Effort vs Impact:**
```
HIGH Impact, LOW Effort:  Phase 1 + 2 (Packet Capture + Export)
HIGH Impact, HIGH Effort: Phase 4 (Replay UI)
LOW Impact, LOW Effort:   Phase 5 (Optimization)
```

---

**Status:** 🟡 **WAITING FOR DECISION**
**Next Step:** Diskussion über Prioritäten und Ressourcen
**ETA (Phase 1+2):** 1-2 Wochen
**ETA (Komplett):** 4-6 Wochen
