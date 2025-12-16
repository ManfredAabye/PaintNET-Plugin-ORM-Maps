# ORM Map Generator für Paint.NET - Installation

## Plugin-Dateien

Dieses Verzeichnis enthält das fertige Paint.NET Plugin für ORM Map Generierung mit Material Presets.

## Kompilierung

### Voraussetzungen:
- Visual Studio 2019 oder neuer
- .NET Framework 4.7.2 oder höher
- Paint.NET installiert (bevorzugt unter `C:\Program Files\paint.net\`)

### Schritte:

1. **Öffne die Solution in Visual Studio:**
   ```
   Öffne ORMGenerator.csproj in Visual Studio
   ```

2. **Anpassen der Paint.NET Pfade (falls nötig):**
   - Öffne `ORMGenerator.csproj`
   - Prüfe die Referenzpfade zu Paint.NET DLLs
   - Standard: `C:\Program Files\paint.net\`
   - Bei Bedarf anpassen

3. **Projekt kompilieren:**
   - Build → Build Solution (oder F6)
   - Wähle "Release" Konfiguration

4. **DLL installieren:**
   - Die kompilierte DLL wird automatisch nach `C:\Program Files\paint.net\Effects\` kopiert
   - Alternativ manuell kopieren:
     ```
     Kopiere: ORMGenerator\bin\Release\ORMGenerator.dll
     Nach: C:\Program Files\paint.net\Effects\
     ```

5. **Paint.NET neustarten**

## Verwendung

1. Öffne ein Bild in Paint.NET
2. Gehe zu: **Effects → Render → ORM Map Generator**
3. Wähle ein Material Preset oder stelle manuell ein
4. Klicke **OK** zum Anwenden

## Features

### Material Presets:
- **Metalle:** Polished Metal, Rough Metal, Brushed Metal
- **Holz:** Wood, Rough Wood
- **Stein:** Stone, Concrete
- **Kunststoffe:** Plastic, Rubber
- **Sonstiges:** Glass, Fabric, Leather, Ceramic, Paint

### Modi:
- **Auto Detection:** Automatische ORM-Generierung aus Bild
- **Manual Mode:** Volle Kontrolle über alle Parameter
- **Preview Mode:** Vorschau der ORM Map

### ORM Format:
- **R-Kanal:** Occlusion (Ambient Occlusion)
- **G-Kanal:** Roughness
- **B-Kanal:** Metallic
- **A-Kanal:** Alpha (Original)

## Fehlerbehebung

**Plugin erscheint nicht in Paint.NET:**
- Stelle sicher, dass Paint.NET neu gestartet wurde
- Prüfe, ob die DLL im richtigen Ordner liegt
- Prüfe Windows Event Viewer für Fehler

**Kompilierungsfehler:**
- Prüfe die Referenzpfade zu Paint.NET DLLs
- Stelle sicher, dass .NET Framework 4.7.2 installiert ist

## Lizenz

Copyright © 2025 Manfred Aabye
