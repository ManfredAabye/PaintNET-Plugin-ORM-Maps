# Anfänger-Anleitung: OpenSim Terrain mit ORM Terrain Generator erstellen

## Teil 1: Grundlagen verstehen

### Was ist eine Heightmap?

Eine Heightmap (Höhenkarte) ist ein schwarz-weißes Bild, bei dem:

- **Schwarz** = tiefste Stelle (oft 0 Meter)
- **Weiß** = höchste Stelle (oft 100 Meter)
- **Grautöne** = Höhen dazwischen

### Wichtige Maße

- **1 OpenSim-Region** = 256 × 256 bis 4096 × 4096 Meter
- **1 Pixel** in der Heightmap = **1 Meter** in OpenSim
- **Gängige Regiongrößen** (in 256er-Schritten):
  - Klein: 256 × 256 Pixel
  - Mittel: 512 × 512 oder 768 × 768 Pixel
  - Groß: 1024 × 1024 oder 1536 × 1536 Pixel
  - Sehr groß: 2048 × 2048, 3072 × 3072 oder 4096 × 4096 Pixel
  - **Hinweis**: Jede Größe in 256er-Schritten ist möglich (256, 512, 768, 1024, 1280, 1536...)

Wichtig im Prompt der OpenSim Konsole steht der Regionsname sollte dort Root stehen gilt alles was gemacht wird für alle Regionen.

---

## Teil 2: ORM Terrain Generator in Paint.NET verwenden

### Schritt 1: Installation

1. Lade das Plugin von GitHub: <https://github.com/ManfredAabye/PaintNET-Plugin-ORM-Maps>
2. Installiere es in Paint.NET: kopiere die DLLs in den Effects-Ordner von Paint.NET

### Schritt 2: Neue Heightmap erstellen

1. **Datei → Neu** in Paint.NET
2. **Größe wählen** (in 256er-Schritten: 256, 512, 768, 1024, 1280, 1536, 2048... bis 4096):
   - **256 × 256 Pixel** (kleine Region)
   - **512 × 512 oder 768 × 768 Pixel** (mittlere Region)
   - **1024 × 1024 oder 1536 × 1536 Pixel** (große Region)
   - **2048 × 2048 bis 4096 × 4096 Pixel** (sehr große Region)
3. **Hintergrund wählen**:
   - **Für Wasser-Terrain**: RGB 40 (Wasseroberfläche)
   - **Für Land-Terrain**: RGB 50-60 (10-20m über Wasser)
   - **Standard**: RGB 128 (88m über Wasser - sehr hoch)

### Schritt 3: Plugin verwenden

Gehe zu **Effekte → ORM → Terrain Heightmap Generator**

#### Plugin-Einstellungen

Das Plugin hat 7 Einstellungen zur automatischen Terrain-Generierung:

##### Die 7 Einstellungen

1. **Zufalls-Seed** (0-99999)
   - Seed für die Zufallsgenerierung
   - 0 = komplett zufällig
   - Gleicher Seed = gleiches Terrain (wiederholbar)

2. **Inselgröße** (0.2-0.8)
   - Standardwert: 0.4
   - Bestimmt die Größe der generierten Insel
   - Kleinere Werte = kleinere Insel
   - Größere Werte = größere Insel

3. **Berg-Intensität** (0.0-1.0)
   - Standardwert: 0.5
   - Wie stark ausgeprägt die Berge sind
   - 0.0 = flach
   - 1.0 = sehr bergig

4. **Erosions-Iterationen** (0-10)
   - Standardwert: 3
   - Anzahl der Erosions-Durchgänge
   - Erzeugt sanftere Übergänge
   - Mehr Iterationen = weichere Landschaft

5. **Fluss-Dichte** (0.0-1.0)
   - Standardwert: 0.3
   - Häufigkeit von Flüssen und Seen
   - 0.0 = keine Flüsse
   - 1.0 = viele Flüsse

6. **Rausch-Skalierung** (1.0-10.0)
   - Standardwert: 4.0
   - Skalierung des Perlin-Noise
   - Höhere Werte = feinere Details
   - Niedrigere Werte = gröbere Strukturen

7. **Rausch-Oktaven** (1-8)
   - Standardwert: 4
   - Anzahl der Perlin-Noise Schichten
   - Mehr Oktaven = mehr Details, aber langsamer

#### Wasser und Berge

- **Wasser**: Dunklere Farben (RGB niedriger)
  - RGB 0-39 = unter Wasser (unter der Wasseroberfläche)
  - RGB 40 = Wasseroberfläche (0 Meter Höhe)
- **Land**: Hellere Farben (RGB höher als 40)
  - RGB 41 = 1 Meter über Wasser
  - RGB 255 = 215 Meter über Wasser (255 - 40 = 215)

**Wichtig**: Höhe über Wasser = RGB-Wert - 40

#### Praktische Werte für Anfänger

```text
Schwarz (0,0,0)          = 40 Meter unter Wasser (Meeresgrund)
Dunkelgrau (40,40,40)    = Wasseroberfläche (0m Höhe) Normal Null
Grau (50,50,50)          = 10 Meter über Wasser
Grau (70,70,70)          = 30 Meter über Wasser
Mittelgrau (128,128,128) = 88 Meter über Wasser
Hellgrau (200,200,200)   = 160 Meter über Wasser
Weiß (255,255,255)       = 215 Meter über Wasser (höchster Punkt)
```

**Empfohlene Werte für OpenSim-Terrain:**

- **Wasseroberfläche**: RGB 40 (0 Meter Höhe - Referenzpunkt) Normal Null
- **Tiefster Punkt (Meeresgrund)**: RGB 0-30 (10-40 Meter unter Wasser)
- **Flaches Land**: RGB 41-45 (1-5 Meter über Wasser)
- **Hügel**: RGB 50-70 (10-30 Meter über Wasser)
- **Berge (empfohlen max.)**: RGB 70-90 (30-50 Meter über Wasser)

**Wichtig**: Berge über 30 Meter Höhe (RGB über 70) erscheinen in OpenSim proportional zu groß und können das Terrain verzerren. Für natürliche Landschaften wird empfohlen: RGB 40-90 (0-50 Meter über Wasser). Je größer die Region je höher können Berge sein, aber Vorsicht mit den Proportionen!

**Technische Grenze**: RGB 255 = 215 Meter über Wasser (möglich, aber nicht empfohlen für OpenSim)

### Schritt 4: Terrain generieren

1. **Wähle deine Einstellungen** im Plugin-Dialog
2. **Klicke OK** - das Plugin generiert automatisch eine komplette Heightmap
3. **Nicht zufrieden?** Ändere die Einstellungen und probiere verschiedene Seeds

**Empfohlene Einstellungen für Anfänger:**

- Inselgröße: 0.4
- Berg-Intensität: 0.5
- Erosions-Iterationen: 3-5
- Fluss-Dichte: 0.3
- Rausch-Skalierung: 4.0

### Schritt 5: Nachbearbeitung (optional)

Nutze die Standard-Paint.NET Werkzeuge für Anpassungen:

1. **Pinsel-Werkzeug**: Manuell Bereiche aufhellen (Berge) oder abdunkeln (Täler)
2. **Auswahl-Tools**: Bestimmte Bereiche auswählen und anpassen
3. **Farbverläufe**: Sanfte Übergänge erstellen

#### Wege und Straßen mit Ebenen-Mischmodus hinzufügen

Eine fortgeschrittene Technik für präzise Wege:

1. **Neue Ebene erstellen**: Ebenen → Neue Ebene hinzufügen
2. **Weg zeichnen**: Mit Pinsel oder Linien-Werkzeug den Weg auf der neuen Ebene zeichnen
3. **Ebenen-Eigenschaften öffnen**: F4 oder Doppelklick auf die Ebene
4. **Mischmodus wählen**:
   - **"Addition" oder "Aufhellen"**: Für erhöhte Wege/Straßen (hebt das Terrain an)
   - **"Subtraktion" oder "Abdunkeln"**: Für Flussbetten oder Gräben (senkt das Terrain ab)
5. **Deckkraft anpassen**: Steuert die Intensität des Effekts (z.B. 30-50% für subtile Wege)
6. **Ebenen zusammenführen**: Ebenen → Nach unten zusammenführen

**Vorteil**: Präzise Kontrolle über Höhenänderungen ohne das Basis-Terrain zu verändern.

### Schritt 6: Weichzeichnen (wichtig!)

1. **Effekte → Weichzeichnen → Gaußscher Weichzeichner**
2. Radius: **1.5 - 2.0 Pixel**
3. Zweck: Harte Kanten entfernen, natürlicheres Aussehen

**Hinweis**: Das Plugin hat bereits Erosions-Glättung integriert, daher kann dieser Schritt oft übersprungen werden.

### Schritt 7: Exportieren

1. **Datei → Speichern unter...**
2. Format: **PNG**
3. Bit-Tiefe: **24-Bit** (wichtig!)
4. Dateiname: z.B. `mein_terrain.png`

---

## Teil 3: Terrain in OpenSim laden

### Vorbereitung

1. Speichere die PNG-Datei auf dem OpenSim-Server
   - Beispiel: `/home/opensim/terrain/mein_terrain.png`
2. Öffne die OpenSim-Konsole

### Grundbefehl für eine Region

```bash
terrain load /home/opensim/terrain/mein_terrain.png
```

### Für mehrere Regionen (Beispiel 2x2 = 4 Regionen)

```bash
terrain load /home/opensim/terrain/mein_terrain.png 2 2 0 0
```

### Höhen überprüfen

```bash
terrain stats
```

Zeigt minimale und maximale Höhe an.

### Höhen anpassen (wenn nötig)

```bash
terrain rescale 0 40
```

Setzt das Terrain auf 0-40 Meter Höhe (empfohlen für natürliche Proportionen in OpenSim).

**Tipp**: Verwende `terrain rescale 0 50` als Maximum, da höhere Berge in OpenSim oft zu groß wirken.

---

## Teil 4: Terrain-Befehle (Deutsche Erklärung)

### Basis-Befehle

| Befehl | Erklärung | Beispiel |
|--------|-----------|----------|
| **`terrain load <datei>`** | Lädt Terrain aus einer Datei | `terrain load /pfad/terrain.png` |
| **`terrain load-tile`** | Lädt einen Ausschnitt aus einer großen Datei | `terrain load-tile große_karte.png 0 0 256 256` |
| **`terrain save <datei>`** | Speichert aktuelles Terrain | `terrain save /backup/terrain_backup.png` |
| **`terrain save-tile`** | Speichert Terrain in eine größere Datei | `terrain save-tile gesamtkarte.png 100 100` |
| **`terrain fill <wert>`** | Füllt komplettes Terrain mit einer Höhe | `terrain fill 20` (alles auf 20m) |
| **`terrain elevate <wert>`** | Erhöht gesamtes Terrain | `terrain elevate 5` (+5 Meter) |
| **`terrain lower <wert>`** | Senkt gesamtes Terrain | `terrain lower 3` (-3 Meter) |
| **`terrain multiply <wert>`** | Multipliziert alle Höhen | `terrain multiply 1.5` (50% höher) |

### Fortgeschrittene Befehle

| Befehl | Erklärung | Beispiel |
|--------|-----------|----------|
| **`terrain bake`** | Speichert Terrain dauerhaft | `terrain bake` |
| **`terrain revert`** | Lädt gespeichertes Terrain zurück | `terrain revert` |
| **`terrain newbrushes`** | Aktiviert neue Terrain-Werkzeuge | `terrain newbrushes on` |
| **`terrain show <x> <y>`** | Zeigt Höhe an Position | `terrain show 128 128` |
| **`terrain stats`** | Zeigt Terrain-Informationen | `terrain stats` |
| **`terrain effect <effekt>`** | Führt Terrain-Effekt aus | `terrain effect smooth` |
| **`terrain flip <x|y>`** | Spiegelt Terrain | `terrain flip x` |
| **`terrain rescale <min> <max>`** | Skaliert Höhenbereich | `terrain rescale 0 30` |
| **`terrain min <wert>`** | Setzt Mindesthöhe | `terrain min 10` |
| **`terrain max <wert>`** | Setzt Maximalhöhe | `terrain max 50` |
| **`terrain modify <befehl>`** | Bereichs-Bearbeitung | `terrain modify raise 10 10 20 20 5` |

### Multi-Region Befehle

```bash
# Für mehrere Regionen aus einer Datei:
terrain load grosse_karte.png 4 4 0 0
# Erklärung: Lädt 4x4 Regionen, startend bei Region (0,0)
```

### Terrain-Texturen setzen

```bash
# Textur-Höhen für Ecken setzen:
set terrain heights <ecke> <min> <max> [x] [y]
# Ecken: SW=0, NW=1, SE=2, NE=3

# Beispiel: Südwest-Ecke auf 0-20 Meter setzen:
set terrain heights 0 0 20

# Terrain-Textur setzen:
set terrain texture <nummer> <uuid>
# Beispiel erste Textur setzen:
set terrain texture 0 00000000-0000-0000-0000-000000000001
```

---

## Teil 5: Tipps für Anfänger

### 1. **Immer zuerst testen!**

- Erstelle eine kleine Test-Region
- Probiere einfache Heightmaps aus
- Verwende `terrain save` um Backups zu machen

### 2. **Höhen kontrollieren:**

- **Wasseroberfläche**: RGB 40 (0 Meter - Referenzhöhe)
- Technisch möglich: -40 bis +215 Meter (RGB 0-255)
- **Empfohlen für OpenSim**: RGB 40-90 (0-50 Meter über Wasser)
- **Grund**: Berge über 30 Meter (RGB > 70) erscheinen proportional zu groß und verzerren das Terrain
- **Formel**: Höhe Wasser = RGB-Wert - 40

### 3. **Fehlerbehebung:**

- **Terrain erscheint nicht?** → Simulator neustarten
- **Falsche Höhen?** → `terrain rescale` verwenden
- **Datei nicht gefunden?** → Vollständigen Pfad angeben

### 4. **Optimaler Workflow:**

   1. In Paint.NET mit ORM-Plugin erstellen
   2. Weichzeichnen (Gaußscher Filter)
   3. Als 24-Bit PNG speichern
   4. Auf Server kopieren
   5. `terrain load` ausführen
   6. Mit `terrain stats` prüfen
   7. Bei Bedarf mit `terrain rescale` anpassen

### 5. **Empfohlene Einstellungen für den Anfang:**

- **Bildgröße** (in 256er-Schritten möglich):
  - Anfänger: 256×256 Pixel
  - Fortgeschritten: 512×512, 768×768 oder 1024×1024 Pixel
  - Experten: 1536×1536, 2048×2048 oder größer bis 4096×4096 Pixel
- **Höhenbereich**: RGB 40-90 (0-50 Meter über Wasser) - optimal für OpenSim
- **Wasseroberfläche**: RGB 40 (0 Meter Referenzhöhe)
- Weichzeichnen: Radius 1.5-2.0
- Export: 24-Bit PNG

**Wichtig**: Berge über 30-40 Meter wirken in OpenSim oft zu groß und verzerren die Proportionen!

**Hinweis**: Größere Regionen benötigen mehr Rechenleistung und Speicher in OpenSim!

---

## Teil 6: Beispiel-Projekt

### Einfache Insel erstellen

1. **Hintergrund**: Dunkelgrau RGB 40 (Wasseroberfläche = 0 Meter Höhe)
2. **Inselform**: Ovale, hellere Fläche in der Mitte
3. **Berg in der Mitte**: Weißer Kreis
4. **Strand**: Hellgrauer Ring um die Insel
5. **Weichzeichnen**: Radius 2.0
6. **Exportieren**: `insel.png`
7. **In OpenSim laden**: `terrain load /pfad/insel.png`
8. **Höhen prüfen**: `terrain stats`
9. **Anpassen**: `terrain rescale 0 40`
