# PaintnetORM - PBR Material Creation Suite for Paint.NET

Eine umfassende Plugin-Suite für Paint.NET 5, die speziell für die Erstellung von PBR-Materialien (Physically Based Rendering) und prozeduralen Terrain-/Map-Generierung entwickelt wurde.

## 📋 Inhaltsverzeichnis

- [Installation](#installation)
- [Plugin-Übersicht](#plugin-übersicht)
  - [Terrain-Generierung](#terrain-generierung)
  - [Straßen & Wege](#straßen--wege)
  - [Vegetation & Gebäude](#vegetation--gebäude)
  - [PBR-Material-Generierung](#pbr-material-generierung)
  - [Map-Analyse & Bearbeitung](#map-analyse--bearbeitung)
  - [Spezialeffekte](#spezialeffekte)
- [Workflow-Beispiele](#workflow-beispiele)

## Installation

1. Kompiliere alle Plugins mit dem Build-Skript:

   ```powershell
   .\Build-And-Copy.ps1
   ```

   oder

   ```batch
   ORM-Copy.bat
   ```

2. Kopiere die DLLs aus `upload\bin\Release\` nach:

   ```bash
   C:\Program Files\paint.net\Effects\
   ```

3. Starte Paint.NET neu

## Plugin-Übersicht

### Terrain-Generierung

#### ORMTerrainGenerator

Generiert prozedurale Heightmaps mit verschiedenen Noise-Algorithmen.

**Parameter:**

- **Noise Type**: Perlin, Simplex, Voronoi
- **Octaves**: Anzahl der Noise-Ebenen (1-8)
- **Persistence**: Amplitude-Reduktion pro Oktave
- **Lacunarity**: Frequenz-Multiplikator pro Oktave
- **Scale**: Zoom-Level des Terrains
- **Seed**: Zufallsseed für Reproduzierbarkeit

**Verwendung:**

1. Neue Ebene erstellen
2. Effects → ORM → Terrain Generator
3. Parameter anpassen bis gewünschtes Terrain entsteht
4. Als Heightmap speichern

---

#### ORM-HeightmapErosion

Simuliert realistische Erosion auf Heightmaps.

**Erosionstypen:**

- **Hydraulic**: Wasser-Erosion in Tälern und Flüssen
- **Thermal**: Hangrutsch-Simulation an steilen Hängen
- **Wind**: Wind-Erosion für Wüstenlandschaften

**Parameter:**

- **Erosion Type**: Art der Erosion
- **Iterations**: Anzahl der Simulationsschritte (mehr = stärker)
- **Strength**: Intensität der Erosion

**Workflow:**

1. Heightmap laden/generieren
2. Effects → ORM → Heightmap Erosion
3. Erosionstyp wählen
4. Iterationen erhöhen für stärkere Effekte

---

#### ORM-CoastlineGenerator

Erzeugt realistische Küstenlinien mit Stränden, Klippen und Inseln.

**Coastline-Typen:**

- **Smooth**: Sanfte, geschwungene Küsten
- **Rocky**: Felsige, zerklüftete Küsten
- **Cliffs**: Steile Klippen
- **Beaches**: Sandstrände mit flachem Übergang
- **Islands**: Inselgruppen

**Parameter:**

- **Coastline Type**: Art der Küste
- **Water Level**: Meeresspiegel-Höhe
- **Detail**: Detailgrad der Küstenlinie
- **Seed**: Zufallsseed

---

### Straßen & Wege

#### ORM-StreetGenerator

Generiert Straßennetze und Verkehrswege.

**Street Patterns:**

- **Grid**: Rechtwinkliges Gitternetz (Manhattan-Style)
- **Radial**: Sternförmige Straßen (Paris-Style)
- **Organic**: Organische, gewundene Straßen
- **Highway**: Autobahnen und Hauptverkehrsachsen
- **Mixed**: Kombination verschiedener Muster

**Parameter:**

- **Pattern**: Straßenmuster
- **Density**: Dichte des Straßennetzes
- **Width**: Straßenbreite
- **Seed**: Zufallsseed

**Verwendung:**

1. Terrain-Heightmap als Basis laden
2. Effects → ORM → Street Generator
3. Pattern und Dichte wählen
4. Auf separater Ebene rendern

---

#### ORM-PathGenerator

Erzeugt Wanderwege, Pfade und Trails.

**Path-Typen:**

- **Hiking**: Bergwanderwege (folgen Höhenlinien)
- **Dirt**: Feldwege
- **Stone**: Gepflasterte Wege
- **Boardwalk**: Stege und Holzwege

**Parameter:**

- **Path Type**: Art des Weges
- **Windiness**: Kurvengrad (0-100)
- **Width**: Pfadbreite
- **Count**: Anzahl der Pfade

**Workflow:**

1. Heightmap laden
2. Effects → ORM → Path Generator
3. Hiking Trail wählt automatisch sinnvolle Routen

---

#### ORM-RiversGenerator

Generiert realistische Flusssysteme mit Nebenflüssen.

**Flow Patterns:**

- **Downhill**: Folgt der Gravitation (nutzt Heightmap)
- **Meandering**: Mäandrierende, geschwungene Flüsse
- **Straight**: Gerade Flussläufe
- **Branching**: Verzweigte Flusssysteme
- **Delta**: Flussdeltas mit mehreren Armen

**Parameter:**

- **Flow Pattern**: Flussmuster
- **Width**: Flussbreite
- **Branches**: Anzahl der Nebenflüsse
- **Seed**: Zufallsseed

**Tipp:** Kombiniere mit Heightmap für realistisches Downhill-Verhalten!

---

### Vegetation & Gebäude

#### ORM-VegetationGenerator

Platziert Vegetation auf Basis von Heightmaps und Biomen.

**Vegetation-Typen:**

- **Dense Forest**: Dichter Wald
- **Sparse Forest**: Lichter Wald
- **Grassland**: Grasland
- **Bushes**: Büsche und Sträucher
- **Mixed**: Gemischte Vegetation

**Parameter:**

- **Vegetation Type**: Vegetationsart
- **Density**: Dichte der Vegetation
- **Min/Max Height**: Höhenbereich für Platzierung
- **Seed**: Zufallsseed

---

#### ORM-BuildingGenerator

Generiert Städte und Gebäudestrukturen.

**Building-Typen:**

- **Residential**: Wohngebiete
- **Commercial**: Geschäftsviertel
- **Industrial**: Industriegebiete
- **Medieval**: Mittelalterliche Stadtstrukturen

**Parameter:**

- **Building Type**: Gebäudeart
- **Density**: Bebauungsdichte
- **Height Variation**: Höhenvariation der Gebäude
- **Seed**: Zufallsseed

---

#### ORM-CaveSystemGenerator

Erzeugt unterirdische Höhlensysteme mit Cellular Automata.

**Parameter:**

- **Density**: Höhlendichte
- **Iterations**: Automata-Iterationen
- **Tunnel Width**: Tunnelbreite
- **Chamber Size**: Größe der Höhlenräume

**Verwendung:**
Perfekt für Dungeon-Maps, Höhlen oder unterirdische Level!

---

### PBR-Material-Generierung

#### ORMNormalmap

Konvertiert Heightmaps in Normal-Maps für 3D-Rendering.

**Parameter:**

- **Strength**: Intensität der Normalen
- **Invert Y**: Y-Achse invertieren (für verschiedene Engines)
- **Wrap**: Seamless-Modus für Kacheln

---

#### ORM-AmbientOcclusionGenerator

Generiert Ambient Occlusion Maps aus Heightmaps.

**Parameter:**

- **Sample Radius**: Abtastradius für AO-Berechnung
- **Intensity**: AO-Intensität
- **Samples**: Anzahl der Samples (mehr = genauer, langsamer)

**Verwendung:**

1. Heightmap laden
2. Effects → ORM → Ambient Occlusion
3. Auf separater Ebene als AO-Map speichern

---

#### ORM-CurvatureGenerator

Erzeugt Konvex/Konkav-Curvature Maps für Detail-Shading.

**Kanäle:**

- **R**: Convex-Bereiche (erhöht)
- **G**: Concave-Bereiche (vertieft)
- **B**: Neutral

**Parameter:**

- **Radius**: Abtastradius
- **Intensity**: Curvature-Intensität

**Anwendung:** Für Weathering-Effekte und Detail-Shading in 3D-Engines

---

#### ORM-DisplacementGenerator

Generiert Displacement-Maps für Tessellation.

**Parameter:**

- **Intensity**: Displacement-Stärke
- **Scale**: Skalierung
- **Direction**: Displacement-Richtung (Normal/Planar)

---

#### ORM-DetailNoiseGenerator

Fügt Mikro-Details zu Texturen hinzu.

**Noise-Typen:**

- **Perlin**: Klassisches Perlin Noise
- **Simplex**: Verbessertes Simplex Noise
- **Cellular**: Voronoi-ähnliches Cellular Noise
- **Fractal**: Multi-Oktaven Fractal Noise

**Parameter:**

- **Noise Type**: Noise-Algorithmus
- **Scale**: Detailgröße
- **Octaves**: Anzahl der Noise-Ebenen
- **Persistence**: Amplitude-Dämpfung

---

#### ORM-MapCombiner

Kombiniert separate Maps zu ORM-packed RGB-Texturen.

**Channel-Mapping:**

- **R**: Occlusion (AO)
- **G**: Roughness
- **B**: Metallic

**Verwendung:**

1. Drei separate Maps als Ebenen laden
2. Effects → ORM → Map Combiner
3. Kanäle zuweisen
4. Als RGB-Textur exportieren

**Tipp:** Standard für Game-Engines wie Unreal Engine!

---

#### ORMEmissive

Erzeugt Emissive-Maps für leuchtende Materialien.

**Verwendung:**

- Straßenlaternen
- Leuchtende Fenster
- Lava-Texturen
- Sci-Fi-Elemente

---

### Map-Analyse & Bearbeitung

#### ORM-TerrainAnalyzer

Analysiert Terrain-Eigenschaften und visualisiert sie.

**Analyse-Modi:**

- **Slope**: Hangneigung (wichtig für Vegetation)
- **Drainage**: Wasserabfluss-Analyse
- **Flow**: Strömungsrichtung
- **Aspect**: Himmelsrichtung der Hänge

**Parameter:**

- **Analysis Type**: Analyse-Modus
- **Visualization**: Farbschema für Darstellung

**Workflow:**

1. Heightmap laden
2. Effects → ORM → Terrain Analyzer
3. Slope-Analyse für Vegetation-Placement
4. Flow-Analyse für Flussplatzierung

---

#### ORM-Splatmapper

Generiert Texture-Splatmaps für Terrain-Shading.

**Layer-Zuweisung:**

- **Layer 0**: Tiefland/Wasser
- **Layer 1**: Ebenen
- **Layer 2**: Hügel
- **Layer 3**: Berge/Gipfel

**Parameter:**

- **Height Thresholds**: Höhenschwellen pro Layer
- **Slope Threshold**: Steigungsschwelle
- **Blend Range**: Übergangsbereich

**Verwendung in Unity/Unreal:**

1. Splatmap generieren
2. Als RGBA-Textur exportieren
3. Im Terrain-Shader verwenden

---

#### ORMHeightmap

Konvertiert zwischen verschiedenen Heightmap-Formaten.

**Features:**

- 8-bit zu 16-bit Konvertierung
- Normalisierung
- Höhenbereich anpassen

---

#### ORMTerrainSplitter

Teilt große Terrains in Kacheln.

**Parameter:**

- **Tile Size**: Kachelgröße (z.B. 1024×1024)
- **Overlap**: Überlappung für nahtlose Übergänge

**Verwendung:**
Für große Open-World-Terrains, die in mehrere Tiles aufgeteilt werden müssen.

---

### Spezialeffekte

#### ORM-BiomeGenerator

Generiert Biom-Zonen basierend auf Höhe und Feuchtigkeit.

**Biome:**

- **Forest**: Wald
- **Grassland**: Grasland
- **Water**: Wasser
- **Desert**: Wüste
- **Snow**: Schnee/Eis
- **Jungle**: Dschungel

**Parameter:**

- **Height Map**: Heightmap als Basis
- **Moisture Map**: Feuchtigkeitskarte
- **Temperature**: Temperaturbereich

**Workflow:**

1. Heightmap generieren
2. Moisture-Map erstellen (Noise)
3. Effects → ORM → Biome Generator
4. Als Color-Map für Terrain verwenden

---

#### ORM-WeatherMapGenerator

Fügt Wettereffekte hinzu.

**Weather-Typen:**

- **Rain**: Regen (Puddles, nasse Oberflächen)
- **Snow**: Schnee
- **Ice**: Eis und Frost
- **Puddles**: Pfützen und Wasserflächen

**Parameter:**

- **Weather Type**: Wetterart
- **Intensity**: Intensität
- **Coverage**: Abdeckung (0-100%)

**Verwendung:**
Perfekt für dynamische Wetter-Overlays oder Seasonal-Variants!

---

### Utilities

#### DHShapeMaker

Vektorbasierter Shape-Editor für präzise Formen.

#### ORMSVG

Importiert SVG-Dateien als Vektorgrafiken.

#### ORMForm

Formular-basierte Effektkonfiguration.

---

## Workflow-Beispiele

### Komplettes Terrain mit Vegetation

1. **Terrain generieren**
   - ORMTerrainGenerator → Simplex Noise
   - ORM-HeightmapErosion → Hydraulic

2. **Vegetation platzieren**
   - ORM-TerrainAnalyzer → Slope-Analyse
   - ORM-VegetationGenerator → Dense Forest (nur flache Bereiche)

3. **Straßen hinzufügen**
   - ORM-StreetGenerator → Organic Pattern
   - ORM-PathGenerator → Hiking Trails

4. **Flüsse einzeichnen**
   - ORM-RiversGenerator → Downhill Flow

### PBR-Material-Pipeline

1. **Base-Textur erstellen** (Foto oder procedural)

2. **Heightmap generieren**
   - ORMTerrainGenerator oder aus Grayscale

3. **Normal-Map erzeugen**
   - ORMNormalmap → Strength: 50%

4. **AO-Map erstellen**
   - ORM-AmbientOcclusionGenerator

5. **Curvature für Details**
   - ORM-CurvatureGenerator

6. **ORM-Packed Texture**
   - ORM-MapCombiner → R=AO, G=Roughness, B=Metallic

### Game-Ready Terrain

1. **Heightmap**: ORMTerrainGenerator
2. **Splatmap**: ORM-Splatmapper (4 Layer)
3. **Biome-Map**: ORM-BiomeGenerator
4. **Detail-Noise**: ORM-DetailNoiseGenerator
5. **Export**: Als 16-bit PNG

---

## 💡 Tipps & Tricks

### Performance

- Große Texturen in mehreren Schritten bearbeiten
- Niedrige Sample-Counts für Previews
- Build-Skript nutzt Release-Optimierung

### Reproduzierbarkeit

- Seeds immer notieren!
- Parameter in Textdatei speichern
- Screenshots der Settings machen

### Seamless Textures

- Wrap-Modus in Terrain-Generator aktivieren
- Kanten mit Offset-Filter prüfen
- Clone-Stamp für manuelle Korrekturen

### Integration in Game-Engines

**Unity:**

- Heightmap als RAW exportieren (16-bit)
- Splatmap als RGBA PNG
- ORM-Textures direkt unterstützt (URP/HDRP)

**Unreal Engine:**

- Heightmap als 16-bit PNG
- Normal-Maps: G-Kanal invertieren!
- ORM-Packed Textures nativ supported

**Godot:**

- Heightmap als EXR (32-bit float)
- Standard-Normal-Maps (Y up)

---

## 🔧 Entwicklung

### Build-System

```powershell
# Alle Plugins kompilieren
.\Build-And-Copy.ps1

# Einzelnes Plugin
dotnet build ORM-StreetGenerator\ORM-StreetGenerator.csproj -c Release
```

### Plugin-Entwicklung

Alle Plugins basieren auf `PropertyBasedEffect`:

```csharp
public class MyPlugin : PropertyBasedEffect
{
    protected override PropertyCollection OnCreatePropertyCollection()
    {
        // Parameter definieren
    }

    protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
    {
        // Parameter auslesen
    }

    protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
    {
        // Rendering-Logik
    }
}
```

---

## Happy Texturing! 🎨
