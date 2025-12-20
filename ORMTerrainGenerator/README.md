# ORM Terrain Heightmap Generator

Ein Paint.NET 5.x Plugin zur Generierung prozeduraler Terrain-Heightmaps mit Inseln, Bergen, Tälern, Flüssen und Seen.

## Features

- **Prozedurale Terrain-Generierung** mit Perlin Noise
- **Inselformen-Auswahl** (Normal/Rund oder Gefüllt)
- **Insel-Generator** mit anpassbarer Größe
- **Berg- und Tal-Systeme** mit konfigurierbarer Intensität
- **Seen** mit natürlicher Platzierung
- **Erosions-Simulation** für realistische, sanfte Übergänge
- **Deterministische Generierung** durch Seed-Kontrolle
- **Heightmap-optimierte Farbgebung** in Graustufen

## Farbschema (Heightmap)

- **Meer**: `#141414` (RGB 20, 20, 20)
- **Seen**: `#272727` (RGB 39, 39, 39)
- **Täler**: `#292929` (RGB 41, 41, 41)
- **Berge**: `#3C3C3C` (RGB 60, 60, 60)
- **Sanfte Übergänge** zwischen allen Höhenstufen

## Installation

### Voraussetzungen

- Paint.NET 5.x
- .NET 9.0 Runtime
- Windows 10/11 (x64 oder ARM64)

### Build

```powershell
# Projekt kompilieren
dotnet build -c Release

# DLL wird automatisch nach C:\Program Files\paint.net\Effects kopiert
```

### Manuelle Installation

1. Kompiliere das Projekt mit `dotnet build -c Release`
2. Kopiere `ORMTerrainGenerator.dll` aus `bin\Release\net9.0-windows\` nach:
   - `C:\Program Files\paint.net\Effects\`
3. Starte Paint.NET neu

## Verwendung

1. Öffne Paint.NET
2. Gehe zu **Effekte → ORM → Terrain Heightmap Generator**
3. Passe die Parameter an:
   - **Inselform**: Normal (Rund) oder Gefüllt (kein Wasser)
   - **Zufalls-Seed**: Kontrolle über die Generierung (0 = zufällig)
   - **Inselgröße**: Größe der generierten Insel (0.2 - 0.8)
   - **Berg-Intensität**: Stärke der Bergformationen (0.0 - 1.0)
   - **Erosions-Iterationen**: Glättung der Übergänge (0 - 10)
   - **Rausch-Skalierung**: Detailgrad der Terrain-Struktur (1.0 - 10.0)
   - **Rausch-Oktaven**: Komplexität des Perlin Noise (1 - 8)
   - **Wasser-Level**: Höhe des Wasserspiegels (0.0 - 100.0)
   - **Strandbreite**: Breite des Strandbereichs (0.0 - 0.3)
   - **Küstenrauheit**: Rauheit der Küstenlinie (0.0 - 1.0)
   - **Berg-Anzahl**: Anzahl der Bergkuppen (0 - 20)
   - **Taltiefe**: Tiefe der Täler zwischen Bergen (0.0 - 1.0)
   - **Seen-Anzahl**: Anzahl der Seen auf der Insel (0 - 20)
   - **Terrain-Typ**: 0=Organisch, 1=Gebirgig, 2=Flach, 3=Archipel
   - **Persistence**: Einfluss höherer Oktaven auf das Rauschen (0.2 - 0.9)

## Technische Details

### Architektur

- **Basis**: `PropertyBasedEffect` von Paint.NET
- **Framework**: .NET 9.0
- **Noise-Algorithmus**: Multi-Oktaven Perlin Noise
- **Erosion**: Nachbarschafts-basierte Glättung
- **Fluss-Generierung**: Höhen-basierte Pfadfindung

### Algorithmen

#### 1. Perlin Noise

Mehrstufiger Perlin Noise für natürliche Terrain-Variation:

- Konfigurierbare Oktaven (1-8)
- Persistence-basierte Amplitude
- Deterministische Hash-Funktion

#### 2. Insel-Formung

Verschiedene Inselformen mit konfigurierbaren Küstenlinien:

- **Normal (Rund)**: Organische runde Form mit variierter Küstenlinie
- **Gefüllt**: Komplett gefüllte Fläche ohne Wasser
- Abfallende Ränder zum Meer
- Küstenrauheit und Strandbreite konfigurierbar

#### 3. Seen-Generierung

Seen werden natürlich in niedriger gelegenen Bereichen platziert:

- Konfigurierbare Anzahl (0-20)
- Natürliche Platzierung in Tälern
- Variable Größe

#### 4. Erosions-Simulation

Iterative Glättung durch Nachbarschafts-Mittelwerte:

- Nur auf Land-Bereichen
- Konfigurierbare Iterations-Anzahl
- Behält Höhen-Struktur bei

### Performance

- ROI-basiertes Rendering
- Heightmap-Cache zwischen Frames
- Optimiert für große Bilder (bis 8K+)

## Verwendung als Heightmap

Die generierten Bilder können direkt als Heightmaps verwendet werden für:

- **Unreal Engine**: Landscape Import
- **Unity**: Terrain Height Import
- **Blender**: Displacement Mapping
- **Game Engines**: Generelle Terrain-Generierung

### Export-Empfehlungen

- **Format**: PNG 16-bit oder EXR für maximale Präzision
- **Größe**: Vielfache von 2^n + 1 (z.B. 1025, 2049, 4097) für optimale Engine-Kompatibilität
- **Kanal**: Grayscale ohne Alpha

## Entwicklung

### Struktur

```bash
ORMTerrainGenerator/
├── ORMTerrainGenerator.cs      # Hauptklasse mit Algorithmen
├── ORMTerrainGenerator.csproj  # Projekt-Datei
└── README.md                    # Diese Datei
```

### Erweiterungen

Mögliche zukünftige Features:

- [ ] Export in 16-bit Grayscale
- [ ] Biome-System (Wüste, Schnee, Dschungel)
- [ ] Vulkan-Generator
- [ ] Canyon-Systeme
- [ ] Höhlen-Generierung (separate Layer)
- [ ] Preset-System
- [ ] Batch-Generierung mit Seed-Liste

## Lizenz

Copyright © 2025 ManfredAabye  
GitHub: <https://github.com/ManfredAabye>

## Changelog

### Version 1.1.0 (2025-12-20)

- **Neu**: Inselformen-Auswahl (Normal/Gefüllt)
- **Entfernt**: Fluss-Generierung (Flussbreite, Flussdichte)
- **Verbessert**: Erweiterte Parameter-Kontrolle
- Gesamt 15 konfigurierbare Parameter

### Version 1.0.0 (2025-12-19)

- Initiales Release
- Perlin Noise Terrain-Generierung
- Insel-Generator
- Seen-System
- Erosions-Simulation
- Heightmap-optimierte Farbgebung
