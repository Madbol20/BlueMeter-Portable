# Datenfluss Redesign - Encounter History

## Aktuelles Problem

Die Art und Weise wie Daten gespeichert werden muss komplett überarbeitet werden.

### Aktueller Ablauf (Problematisch)
```
1. Kampf läuft
   ├─ DpsData wird live aktualisiert
   ├─ Charts werden live aktualisiert
   └─ Daten werden INKREMENTELL gespeichert

2. Kampf endet → "NewSectionCreated" Event
   ├─ DataStorageExtensions.OnNewSectionCreated()
   ├─ SaveCurrentEncounterAsync()
   └─ Daten in DB gespeichert (aber evtl. unvollständig)

3. UI wechselt zu "Last Battle"
   └─ Zeigt letzte Daten an

4. Neuer Kampf startet
   └─ Alte Daten werden überschrieben
```

**Probleme:**
- ❌ Daten werden während des Kampfes gespeichert, nicht am Ende
- ❌ "Last Battle" View hat bereits die Daten gelöscht wenn gespeichert wird
- ❌ Timing-Probleme zwischen Save und Clear
- ❌ Unvollständige Daten wenn Speicherung während Kampf passiert

## Gewünschter Ablauf (Neu)

```
1. Kampf läuft
   ├─ DpsData wird live aktualisiert
   ├─ Charts werden live aktualisiert
   └─ NICHTS wird gespeichert (nur im Memory)

2. Kampf endet → "NewSectionCreated" Event
   ├─ UI wechselt zu "Last Battle" Mode
   ├─ Alle Daten bleiben im Memory erhalten
   └─ NOCH NICHT in DB gespeichert

3. "Last Battle" Mode aktiv
   ├─ User kann letzte Battle reviewen
   ├─ Alle Stats sind sichtbar (DPS, Skills, Charts, etc.)
   └─ Daten sind noch LIVE im Memory

4. Neuer Kampf startet ODER User lädt anderen Encounter
   ├─ JETZT erst: Snapshot aller Daten erstellen
   ├─ SaveEncounterSnapshot() aufrufen
   │  ├─ Snapshot von DpsData Dictionary
   │  ├─ Snapshot von PlayerInfo Dictionary
   │  ├─ Snapshot von Chart History (DPS/HPS)
   │  ├─ Snapshot von Skill Breakdowns
   │  └─ ALLE aggregierten Stats
   ├─ Snapshot in SQLite DB speichern
   └─ Dann erst Memory clearen für neuen Kampf

5. History View öffnen
   ├─ Liste zeigt: Player Count, Duration, Boss Name, etc.
   ├─ User wählt Encounter aus
   ├─ LoadEncounterSnapshot() aufrufen
   │  └─ Lädt ALLE Daten aus DB
   ├─ DPS Meter UI wird mit historischen Daten gefüllt
   │  ├─ Player Liste mit DPS/HPS
   │  ├─ Skill Breakdowns klickbar
   │  ├─ Charts zeigen historische Kurven
   │  └─ Alle Stats wie "Live" angezeigt
   └─ UI zeigt "History Mode" Indicator
```

## Vorteile

✅ **Daten-Vollständigkeit:** Alle Daten werden am Ende des Kampfes als komplettes Paket gespeichert
✅ **Timing-Garantie:** Speicherung passiert VOR dem Löschen
✅ **Last Battle bleibt intakt:** "Last Battle" View hat volle Daten bis neuer Kampf startet
✅ **Performance:** Nur 1x Speicherung am Ende, nicht inkrementell während Kampf
✅ **Einfacheres Debugging:** Klarer Zeitpunkt wann gespeichert wird

## Implementierungs-Plan

### Phase 1: Snapshot-Mechanismus
- [ ] Neue Methode: `CreateEncounterSnapshot()` in DataStorage
  - Erstellt vollständigen Snapshot aller Kampf-Daten
  - Inkludiert: DpsData, PlayerInfo, Charts, Skills, Stats
  - Gibt `EncounterSnapshot` Objekt zurück

### Phase 2: Timing Änderung
- [ ] Verschiebe Save-Call von `OnNewSectionCreated` nach `OnBeforeSectionStart`
  - Alternative: Neues Event "BeforeNewEncounter"
  - Speichert vorherigen Encounter bevor neuer startet

### Phase 3: History Loading Verbesserung
- [ ] `LoadHistoricalEncounter()` lädt Snapshot
- [ ] Populate komplettes DPS Meter UI mit historischen Daten
- [ ] History Mode Indicator in UI
- [ ] "Return to Live" Button

### Phase 4: UI Integration
- [ ] History List zeigt Summary (Players, Duration, Boss)
- [ ] Doppelklick öffnet Encounter im DPS Meter
- [ ] Alle Features funktionieren (Skills, Charts, etc.)
- [ ] Smooth transition zwischen History ↔ Live

## Datenstruktur

### EncounterSnapshot (Neues Modell)
```csharp
public class EncounterSnapshot
{
    public string EncounterId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long DurationMs { get; set; }
    public string? BossName { get; set; }

    // Complete data snapshots
    public Dictionary<long, PlayerInfo> PlayerInfos { get; set; }
    public Dictionary<long, DpsData> DpsData { get; set; }
    public Dictionary<long, List<ChartDataPoint>> DpsHistory { get; set; }
    public Dictionary<long, List<ChartDataPoint>> HpsHistory { get; set; }

    // Aggregate stats (pre-calculated)
    public long TotalDamage { get; set; }
    public long TotalHealing { get; set; }
    public int PlayerCount { get; set; }
}
```

## Migration Path

Keine Breaking Changes - alte Encounters bleiben kompatibel!

1. ✅ Schema ist bereits richtig (alle Felder existieren)
2. ✅ Nur Timing der Save-Calls muss geändert werden
3. ✅ Loading-Logik bleibt fast gleich
4. ✅ Keine Daten gehen verloren

## Testing Plan

- [ ] Test: Encounter endet → Last Battle zeigt volle Daten
- [ ] Test: Neuer Encounter startet → Alter wird gespeichert
- [ ] Test: History List zeigt alle Encounters
- [ ] Test: History Encounter öffnen → Alle Daten sichtbar
- [ ] Test: Skills klickbar in History Mode
- [ ] Test: Charts zeigen korrekte historische Daten
- [ ] Test: Return to Live funktioniert
