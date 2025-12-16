# Paint.NET Plugin für ORM Maps

## **Voraussetzungen**

1. **Visual Studio** (2019 oder neuer)
2. **Paint.NET Plugin Template**
3. **.NET Framework 4.7.2 oder höher**

## **Schritt-für-Schritt Anleitung**

### **1. Entwicklungsumgebung einrichten**

```xml
<!-- 1. Paint.NET Plugin Template installieren -->
- Visual Studio → Extensions → "paint.net Plugin Template" suchen
- Oder: https://github.com/paintdotnet/PluginTemplate
```

### **2. Neues Plugin-Projekt erstellen**

```csharp
// In Visual Studio:
// 1. Neues Projekt → Paint.NET Effect
// 2. Projekt benennen (z.B. "ORMMapGenerator")
```

### **3. Basis-Plugin-Struktur**

```csharp
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System.Drawing;

namespace ORMMapGenerator
{
    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public class ORMMapGenerator : PropertyBasedEffect
    {
        public ORMMapGenerator()
            : base("ORM Map Generator", null, SubmenuNames.Photo, 
                   new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }
        
        protected override PropertyCollection OnCreatePropertyCollection()
        {
            // Hier kommen deine Einstellungen/UI-Elemente
            List<Property> props = new List<Property>();
            
            // Beispiel: Schieberegler für Roughness
            props.Add(new DoubleProperty("Roughness", 0.5, 0.0, 1.0));
            props.Add(new DoubleProperty("Metallic", 0.0, 0.0, 1.0));
            
            return new PropertyCollection(props);
        }
        
        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            // UI erstellen
            ControlInfo configUI = CreateDefaultConfigUI(props);
            
            configUI.SetPropertyControlValue("Roughness", ControlInfoPropertyNames.DisplayName, "Roughness");
            configUI.SetPropertyControlValue("Metallic", ControlInfoPropertyNames.DisplayName, "Metallic");
            
            return configUI;
        }
    }
}
```

### **4. ORM Map Generation Logik**

```csharp
protected override void OnRender(Rectangle[] renderRects, 
                                 int startIndex, 
                                 int length)
{
    // Eingabebild verarbeiten
    Surface inputSurface = this.EnvironmentParameters.SourceSurface;
    Surface outputSurface = this.EnvironmentParameters.DestinationSurface;
    
    // Eigenschaften aus UI lesen
    double roughness = this.Token.GetProperty<DoubleProperty>("Roughness").Value;
    double metallic = this.Token.GetProperty<DoubleProperty>("Metallic").Value;
    
    for (int i = startIndex; i < startIndex + length; ++i)
    {
        Rectangle rect = renderRects[i];
        
        for (int y = rect.Top; y < rect.Bottom; y++)
        {
            for (int x = rect.Left; x < rect.Right; x++)
            {
                ColorBgra sourcePixel = inputSurface[x, y];
                
                // ORM Map erstellen:
                // R-Kanal = Roughness
                // G-Kanal = Metallic  
                // B-Kanal = Leer (0) oder Ambient Occlusion
                // A-Kanal = Original-Alpha
                
                byte roughnessValue = (byte)(roughness * 255);
                byte metallicValue = (byte)(metallic * 255);
                
                ColorBgra outputPixel = ColorBgra.FromBgra(
                    0,              // B-Kanal
                    metallicValue,  // G-Kanal = Metallic
                    roughnessValue, // R-Kanal = Roughness
                    sourcePixel.A   // Alpha beibehalten
                );
                
                // Alternative: Automatische Generierung aus Helligkeit
                // float brightness = sourcePixel.GetIntensity();
                // byte aoValue = (byte)(brightness * 255);
                
                outputSurface[x, y] = outputPixel;
            }
        }
    }
}
```

### **5. Fortgeschrittene Version mit Auto-Detection**

```csharp
public class AdvancedORMMapGenerator : PropertyBasedEffect
{
    private enum MapType { Roughness, Metallic, AmbientOcclusion, Combined }
    
    protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
    {
        Surface src = this.EnvironmentParameters.SourceSurface;
        Surface dst = this.EnvironmentParameters.DestinationSurface;
        
        for (int i = startIndex; i < startIndex + length; ++i)
        {
            Rectangle rect = renderRects[i];
            
            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    ColorBgra pixel = src[x, y];
                    
                    // Automatische ORM-Erkennung basierend auf Farben
                    float intensity = pixel.GetIntensity();
                    float saturation = pixel.GetSaturation();
                    
                    // Roughness aus Helligkeit (dunkel = rau, hell = glatt)
                    byte roughness = (byte)((1.0f - intensity) * 255);
                    
                    // Metallic aus Sättigung (gesättigte Farben = nicht metallisch)
                    byte metallic = (byte)((saturation < 0.2f) ? 255 : 0);
                    
                    // Ambient Occlusion aus Rot-Kanal oder Helligkeit
                    byte ao = pixel.R;
                    
                    dst[x, y] = ColorBgra.FromBgra(ao, metallic, roughness, pixel.A);
                }
            }
        }
    }
}
```

### **6. DLL erstellen und installieren**

1. **Projekt kompilieren** (Release-Modus)
2. **DLL in Paint.NET Plugin-Ordner kopieren**:

    ```bash
    C:\Program Files\paint.net\Effects\
    ```

3. **Paint.NET neustarten**

## **Hilfreiche Ressourcen**

### **Paint.NET Plugin Dokumentation:**

- [Offizielle Dokumentation](https://www.getpaint.net/doc/latest/DevelopmentPlugins.html)
- [API Reference](https://docs.getpaint.net/api/index.html)
- [CodeLab Plugin](https://forums.getpaint.net/topic/17068-codelab-v36-april-20-2020/) - Visueller Plugin-Editor

### **Beispiel-Plugins zum Lernen:**

```plaintext
1. "NormalMapPlus" - Für Normal Maps
2. "BoltBait's Plugin Pack" - Viele Beispiele
3. "Ed Harvey Effects" - Fortgeschrittene Techniken
```

## **Tipps für dein ORM Plugin:**

1. **Einstellungen hinzufügen:**
   - Roughness/Metallic Slider
   - Ambient Occlusion Toggle
   - Invert Optionen
   - Presets für Common Materials

2. **Vorschau-Funktion** implementieren

3. **Mehrere Eingabebilder** unterstützen (separate Texturen)

4. **Batch-Verarbeitung** für mehrere Dateien

---

Hier ist ein komplettes Paint.NET Plugin für ORM Map Generation:

## **1. Projektstruktur**

```bash
ORMGenerator/
│
├── ORMGenerator.csproj
├── ORMGenerator.cs
├── ORMGeneratorUI.cs
├── AssemblyInfo.cs
├── Properties/
│   └── Resources.resx
└── README.txt
```

## **2. ORMGenerator.csproj**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" />
  
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Release</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{YOUR-GUID-HERE}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>ORMGenerator</RootNamespace>
    <AssemblyName>ORMGenerator</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <Deterministic>true</Deterministic>
    <NuGetPackageImportStamp></NuGetPackageImportStamp>
  </PropertyGroup>
  
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  
  <ItemGroup>
    <Reference Include="PaintDotNet.Core, Version=4.300.0.0, Culture=neutral, PublicKeyToken=null">
      <HintPath>..\..\..\..\Program Files\paint.net\PaintDotNet.Core.dll</HintPath>
    </Reference>
    <Reference Include="PaintDotNet.Effects, Version=4.300.0.0, Culture=neutral, PublicKeyToken=null">
      <HintPath>..\..\..\..\Program Files\paint.net\PaintDotNet.Effects.dll</HintPath>
    </Reference>
    <Reference Include="PaintDotNet.Resources, Version=4.300.0.0, Culture=neutral, PublicKeyToken=null">
      <HintPath>..\..\..\..\Program Files\paint.net\PaintDotNet.Resources.dll</HintPath>
    </Reference>
    <Reference Include="System" />
    <Reference Include="System.Core" />
    <Reference Include="System.Drawing" />
    <Reference Include="System.Windows.Forms" />
  </ItemGroup>
  
  <ItemGroup>
    <Compile Include="AssemblyInfo.cs" />
    <Compile Include="ORMGenerator.cs" />
    <Compile Include="ORMGeneratorUI.cs" />
    <Compile Include="Properties\Resources.Designer.cs">
      <AutoGen>True</AutoGen>
      <DesignTime>True</DesignTime>
      <DependentUpon>Resources.resx</DependentUpon>
    </Compile>
  </ItemGroup>
  
  <ItemGroup>
    <EmbeddedResource Include="Properties\Resources.resx">
      <Generator>ResXFileCodeGenerator</Generator>
      <LastGenOutput>Resources.Designer.cs</LastGenOutput>
    </EmbeddedResource>
  </ItemGroup>
  
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

## **3. ORMGenerator.cs - Hauptklasse**

```csharp
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace ORMGenerator
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author => "Your Name";
        public string Copyright => "Copyright © 2024";
        public string DisplayName => "ORM Map Generator";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public Uri WebsiteUri => new Uri("https://github.com/yourusername");
    }

    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public class ORMGenerator : PropertyBasedEffect
    {
        private Surface sourceSurface;
        private double roughness;
        private double metallic;
        private double ambientOcclusion;
        private bool useAutoDetection;
        private bool invertRoughness;
        private bool invertMetallic;
        private bool invertAO;
        private MapSource roughnessSource;
        private MapSource metallicSource;
        private MapSource aoSource;
        private bool previewMode;

        public enum MapSource
        {
            RedChannel,
            GreenChannel,
            BlueChannel,
            AlphaChannel,
            Grayscale,
            CustomValue
        }

        public ORMGenerator()
            : base(
                "ORM Map Generator", 
                null, 
                SubmenuNames.Adjustments, 
                new EffectOptions() 
                { 
                    Flags = EffectFlags.Configurable | EffectFlags.SingleRenderCall,
                    RenderingSchedule = EffectRenderingSchedule.None
                })
        {
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            // Auto Detection
            props.Add(new BooleanProperty("AutoDetection", true));
            
            // Roughness Properties
            List<object> roughnessSources = new List<object>
            {
                MapSource.RedChannel,
                MapSource.GreenChannel,
                MapSource.BlueChannel,
                MapSource.AlphaChannel,
                MapSource.Grayscale,
                MapSource.CustomValue
            };
            props.Add(new StaticListChoiceProperty("RoughnessSource", roughnessSources, 4));
            props.Add(new DoubleProperty("Roughness", 0.5, 0.0, 1.0));
            props.Add(new BooleanProperty("InvertRoughness", false));
            
            // Metallic Properties
            List<object> metallicSources = new List<object>
            {
                MapSource.RedChannel,
                MapSource.GreenChannel,
                MapSource.BlueChannel,
                MapSource.AlphaChannel,
                MapSource.Grayscale,
                MapSource.CustomValue
            };
            props.Add(new StaticListChoiceProperty("MetallicSource", metallicSources, 4));
            props.Add(new DoubleProperty("Metallic", 0.0, 0.0, 1.0));
            props.Add(new BooleanProperty("InvertMetallic", false));
            
            // Ambient Occlusion Properties
            List<object> aoSources = new List<object>
            {
                MapSource.RedChannel,
                MapSource.GreenChannel,
                MapSource.BlueChannel,
                MapSource.AlphaChannel,
                MapSource.Grayscale,
                MapSource.CustomValue
            };
            props.Add(new StaticListChoiceProperty("AOSource", aoSources, 0));
            props.Add(new DoubleProperty("AmbientOcclusion", 1.0, 0.0, 1.0));
            props.Add(new BooleanProperty("InvertAO", false));
            
            // Preview
            props.Add(new BooleanProperty("PreviewMode", false));

            return new PropertyCollection(props);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            // Auto Detection
            configUI.SetPropertyControlValue("AutoDetection", ControlInfoPropertyNames.DisplayName, "Auto Detection");
            configUI.SetPropertyControlValue("AutoDetection", ControlInfoPropertyNames.Description, 
                "Automatisch ORM aus Bild analysieren");
            
            // Roughness Group
            configUI.SetPropertyControlValue("RoughnessSource", ControlInfoPropertyNames.DisplayName, "Roughness Source");
            configUI.SetPropertyControlValue("RoughnessSource", ControlInfoPropertyNames.Description,
                "Quelle für Roughness Map");
            configUI.SetPropertyControlType("RoughnessSource", PropertyControlType.DropDown);
            
            configUI.SetPropertyControlValue("Roughness", ControlInfoPropertyNames.DisplayName, "Roughness Value");
            configUI.SetPropertyControlValue("Roughness", ControlInfoPropertyNames.Description,
                "Globale Roughness (0.0 = glatt, 1.0 = rau)");
            configUI.SetPropertyControlType("Roughness", PropertyControlType.Slider);
            
            configUI.SetPropertyControlValue("InvertRoughness", ControlInfoPropertyNames.DisplayName, "Invert Roughness");
            
            // Metallic Group
            configUI.SetPropertyControlValue("MetallicSource", ControlInfoPropertyNames.DisplayName, "Metallic Source");
            configUI.SetPropertyControlValue("MetallicSource", ControlInfoPropertyNames.Description,
                "Quelle für Metallic Map");
            configUI.SetPropertyControlType("MetallicSource", PropertyControlType.DropDown);
            
            configUI.SetPropertyControlValue("Metallic", ControlInfoPropertyNames.DisplayName, "Metallic Value");
            configUI.SetPropertyControlValue("Metallic", ControlInfoPropertyNames.Description,
                "Globale Metallic (0.0 = nicht metallisch, 1.0 = metallisch)");
            configUI.SetPropertyControlType("Metallic", PropertyControlType.Slider);
            
            configUI.SetPropertyControlValue("InvertMetallic", ControlInfoPropertyNames.DisplayName, "Invert Metallic");
            
            // AO Group
            configUI.SetPropertyControlValue("AOSource", ControlInfoPropertyNames.DisplayName, "AO Source");
            configUI.SetPropertyControlValue("AOSource", ControlInfoPropertyNames.Description,
                "Quelle für Ambient Occlusion");
            configUI.SetPropertyControlType("AOSource", PropertyControlType.DropDown);
            
            configUI.SetPropertyControlValue("AmbientOcclusion", ControlInfoPropertyNames.DisplayName, "AO Value");
            configUI.SetPropertyControlValue("AmbientOcclusion", ControlInfoPropertyNames.Description,
                "Globale Ambient Occlusion");
            configUI.SetPropertyControlType("AmbientOcclusion", PropertyControlType.Slider);
            
            configUI.SetPropertyControlValue("InvertAO", ControlInfoPropertyNames.DisplayName, "Invert AO");
            
            // Preview
            configUI.SetPropertyControlValue("PreviewMode", ControlInfoPropertyNames.DisplayName, "Preview");
            configUI.SetPropertyControlValue("PreviewMode", ControlInfoPropertyNames.Description,
                "Vorschau der ORM Map (deaktiviert für Export)");

            // Gruppierung
            configUI.SetPropertyControlValue("RoughnessSource", ControlInfoPropertyNames.GroupName, "Roughness");
            configUI.SetPropertyControlValue("Roughness", ControlInfoPropertyNames.GroupName, "Roughness");
            configUI.SetPropertyControlValue("InvertRoughness", ControlInfoPropertyNames.GroupName, "Roughness");
            
            configUI.SetPropertyControlValue("MetallicSource", ControlInfoPropertyNames.GroupName, "Metallic");
            configUI.SetPropertyControlValue("Metallic", ControlInfoPropertyNames.GroupName, "Metallic");
            configUI.SetPropertyControlValue("InvertMetallic", ControlInfoPropertyNames.GroupName, "Metallic");
            
            configUI.SetPropertyControlValue("AOSource", ControlInfoPropertyNames.GroupName, "Ambient Occlusion");
            configUI.SetPropertyControlValue("AmbientOcclusion", ControlInfoPropertyNames.GroupName, "Ambient Occlusion");
            configUI.SetPropertyControlValue("InvertAO", ControlInfoPropertyNames.GroupName, "Ambient Occlusion");

            return configUI;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, 
                                                RenderArgs dstArgs, 
                                                RenderArgs srcArgs)
        {
            // Werte aus UI lesen
            this.roughness = newToken.GetProperty<DoubleProperty>("Roughness").Value;
            this.metallic = newToken.GetProperty<DoubleProperty>("Metallic").Value;
            this.ambientOcclusion = newToken.GetProperty<DoubleProperty>("AmbientOcclusion").Value;
            
            this.useAutoDetection = newToken.GetProperty<BooleanProperty>("AutoDetection").Value;
            this.invertRoughness = newToken.GetProperty<BooleanProperty>("InvertRoughness").Value;
            this.invertMetallic = newToken.GetProperty<BooleanProperty>("InvertMetallic").Value;
            this.invertAO = newToken.GetProperty<BooleanProperty>("InvertAO").Value;
            this.previewMode = newToken.GetProperty<BooleanProperty>("PreviewMode").Value;
            
            this.roughnessSource = (MapSource)newToken.GetProperty<StaticListChoiceProperty>("RoughnessSource").Value;
            this.metallicSource = (MapSource)newToken.GetProperty<StaticListChoiceProperty>("MetallicSource").Value;
            this.aoSource = (MapSource)newToken.GetProperty<StaticListChoiceProperty>("AOSource").Value;
            
            this.sourceSurface = srcArgs.Surface;

            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        protected override void OnRender(Rectangle[] renderRects, 
                                         int startIndex, 
                                         int length)
        {
            if (this.useAutoDetection)
            {
                RenderAutoDetection(renderRects, startIndex, length);
            }
            else
            {
                RenderManual(renderRects, startIndex, length);
            }
        }

        private void RenderAutoDetection(Rectangle[] renderRects, int startIndex, int length)
        {
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Rectangle rect = renderRects[i];
                
                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        ColorBgra pixel = this.sourceSurface[x, y];
                        
                        // Automatische Analyse
                        float intensity = pixel.GetIntensity();
                        float saturation = pixel.GetSaturation();
                        float hue = pixel.GetHue() / 360.0f;
                        
                        // Roughness: Dunkle Bereiche = rau, Helle = glatt
                        byte rValue = (byte)((1.0f - intensity) * 255);
                        
                        // Metallic: Niedrige Sättigung + bestimmte Farben = metallisch
                        byte mValue = 0;
                        if (saturation < 0.3f)
                        {
                            // Grautöne sind potenziell metallisch
                            mValue = (byte)((1.0f - (saturation * 3.0f)) * 255);
                        }
                        
                        // Ambient Occlusion: Rot-Kanal oder Intensität
                        byte aoValue = pixel.R;
                        
                        ColorBgra outputPixel;
                        
                        if (this.previewMode)
                        {
                            // Preview: Zeige ORM als Farbbild
                            outputPixel = ColorBgra.FromBgra(
                                (byte)(aoValue * 0.5),  // Blau für AO
                                mValue,                 // Grün für Metallic
                                rValue,                 // Rot für Roughness
                                pixel.A
                            );
                        }
                        else
                        {
                            // Export: ORM Map im korrekten Format
                            outputPixel = ColorBgra.FromBgra(
                                aoValue,
                                mValue,
                                rValue,
                                pixel.A
                            );
                        }
                        
                        this.DstArgs.Surface[x, y] = outputPixel;
                    }
                }
            }
        }

        private void RenderManual(Rectangle[] renderRects, int startIndex, int length)
        {
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Rectangle rect = renderRects[i];
                
                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        ColorBgra pixel = this.sourceSurface[x, y];
                        
                        // Roughness berechnen
                        byte rValue = GetChannelValue(pixel, this.roughnessSource, this.roughness);
                        if (this.invertRoughness) rValue = (byte)(255 - rValue);
                        
                        // Metallic berechnen
                        byte mValue = GetChannelValue(pixel, this.metallicSource, this.metallic);
                        if (this.invertMetallic) mValue = (byte)(255 - mValue);
                        
                        // AO berechnen
                        byte aoValue = GetChannelValue(pixel, this.aoSource, this.ambientOcclusion);
                        if (this.invertAO) aoValue = (byte)(255 - aoValue);
                        
                        ColorBgra outputPixel;
                        
                        if (this.previewMode)
                        {
                            // Preview Modus
                            outputPixel = ColorBgra.FromBgra(
                                (byte)(aoValue * 0.5),
                                mValue,
                                rValue,
                                pixel.A
                            );
                        }
                        else
                        {
                            // Export Modus
                            outputPixel = ColorBgra.FromBgra(
                                aoValue,
                                mValue,
                                rValue,
                                pixel.A
                            );
                        }
                        
                        this.DstArgs.Surface[x, y] = outputPixel;
                    }
                }
            }
        }

        private byte GetChannelValue(ColorBgra pixel, MapSource source, double customValue)
        {
            switch (source)
            {
                case MapSource.RedChannel:
                    return pixel.R;
                case MapSource.GreenChannel:
                    return pixel.G;
                case MapSource.BlueChannel:
                    return pixel.B;
                case MapSource.AlphaChannel:
                    return pixel.A;
                case MapSource.Grayscale:
                    return (byte)(pixel.GetIntensity() * 255);
                case MapSource.CustomValue:
                    return (byte)(customValue * 255);
                default:
                    return 0;
            }
        }

        // Hilfsfunktionen
        public static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
```

## **4. ORMGeneratorUI.cs - Benutzeroberfläche (optional erweitert)**

```csharp
using PaintDotNet;
using PaintDotNet.Controls;
using PaintDotNet.IndirectUI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ORMGenerator
{
    public class ORMGeneratorConfigDialog : EffectConfigDialog
    {
        private CheckBox autoDetectionCheckBox;
        private TrackBar roughnessTrackBar;
        private TrackBar metallicTrackBar;
        private TrackBar aoTrackBar;
        private CheckBox previewCheckBox;
        private PictureBox previewBox;
        private Bitmap previewImage;

        public ORMGeneratorConfigDialog()
        {
            InitializeComponent();
            UpdatePreview();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Auto Detection Checkbox
            this.autoDetectionCheckBox = new CheckBox();
            this.autoDetectionCheckBox.Text = "Auto Detection";
            this.autoDetectionCheckBox.Location = new Point(10, 10);
            this.autoDetectionCheckBox.Size = new Size(150, 20);
            this.autoDetectionCheckBox.CheckedChanged += OnAutoDetectionChanged;
            this.Controls.Add(this.autoDetectionCheckBox);
            
            // Roughness TrackBar
            Label roughnessLabel = new Label();
            roughnessLabel.Text = "Roughness:";
            roughnessLabel.Location = new Point(10, 40);
            roughnessLabel.Size = new Size(80, 20);
            this.Controls.Add(roughnessLabel);
            
            this.roughnessTrackBar = new TrackBar();
            this.roughnessTrackBar.Location = new Point(90, 40);
            this.roughnessTrackBar.Size = new Size(200, 45);
            this.roughnessTrackBar.Minimum = 0;
            this.roughnessTrackBar.Maximum = 100;
            this.roughnessTrackBar.TickFrequency = 10;
            this.roughnessTrackBar.ValueChanged += OnSliderValueChanged;
            this.Controls.Add(this.roughnessTrackBar);
            
            // Metallic TrackBar
            Label metallicLabel = new Label();
            metallicLabel.Text = "Metallic:";
            metallicLabel.Location = new Point(10, 90);
            metallicLabel.Size = new Size(80, 20);
            this.Controls.Add(metallicLabel);
            
            this.metallicTrackBar = new TrackBar();
            this.metallicTrackBar.Location = new Point(90, 90);
            this.metallicTrackBar.Size = new Size(200, 45);
            this.metallicTrackBar.Minimum = 0;
            this.metallicTrackBar.Maximum = 100;
            this.metallicTrackBar.TickFrequency = 10;
            this.metallicTrackBar.ValueChanged += OnSliderValueChanged;
            this.Controls.Add(this.metallicTrackBar);
            
            // AO TrackBar
            Label aoLabel = new Label();
            aoLabel.Text = "Ambient Occlusion:";
            aoLabel.Location = new Point(10, 140);
            aoLabel.Size = new Size(120, 20);
            this.Controls.Add(aoLabel);
            
            this.aoTrackBar = new TrackBar();
            this.aoTrackBar.Location = new Point(130, 140);
            this.aoTrackBar.Size = new Size(160, 45);
            this.aoTrackBar.Minimum = 0;
            this.aoTrackBar.Maximum = 100;
            this.aoTrackBar.TickFrequency = 10;
            this.aoTrackBar.ValueChanged += OnSliderValueChanged;
            this.Controls.Add(this.aoTrackBar);
            
            // Preview Checkbox
            this.previewCheckBox = new CheckBox();
            this.previewCheckBox.Text = "Preview";
            this.previewCheckBox.Location = new Point(10, 190);
            this.previewCheckBox.Size = new Size(80, 20);
            this.previewCheckBox.CheckedChanged += OnPreviewChanged;
            this.Controls.Add(this.previewCheckBox);
            
            // Preview Box
            this.previewBox = new PictureBox();
            this.previewBox.Location = new Point(300, 10);
            this.previewBox.Size = new Size(256, 256);
            this.previewBox.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(this.previewBox);
            
            // Buttons
            Button okButton = new Button();
            okButton.Text = "OK";
            okButton.Location = new Point(10, 220);
            okButton.Size = new Size(75, 23);
            okButton.Click += (s, e) => this.DialogResult = DialogResult.OK;
            this.Controls.Add(okButton);
            
            Button cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(90, 220);
            cancelButton.Size = new Size(75, 23);
            cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(cancelButton);
            
            this.ClientSize = new Size(570, 260);
            this.Text = "ORM Map Generator";
            this.ResumeLayout(false);
        }

        private void OnAutoDetectionChanged(object sender, EventArgs e)
        {
            bool enabled = !this.autoDetectionCheckBox.Checked;
            this.roughnessTrackBar.Enabled = enabled;
            this.metallicTrackBar.Enabled = enabled;
            this.aoTrackBar.Enabled = enabled;
            UpdatePreview();
        }

        private void OnSliderValueChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void OnPreviewChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (this.previewImage != null)
            {
                this.previewImage.Dispose();
            }
            
            // Erstelle eine Vorschau-ORM Map
            this.previewImage = new Bitmap(256, 256);
            using (Graphics g = Graphics.FromImage(this.previewImage))
            {
                // Einfaches Gradienten-Beispiel
                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float r = x / 255.0f;
                        float gVal = y / 255.0f;
                        float b = this.previewCheckBox.Checked ? 0.5f : 1.0f;
                        
                        Color color = Color.FromArgb(
                            (int)(r * 255),
                            (int)(gVal * 255),
                            (int)(b * 255)
                        );
                        
                        this.previewImage.SetPixel(x, y, color);
                    }
                }
            }
            
            this.previewBox.Image = this.previewImage;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (this.previewImage != null)
            {
                this.previewImage.Dispose();
            }
            base.OnClosed(e);
        }
    }
}
```

## **5. AssemblyInfo.cs**

```csharp
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("ORM Map Generator")]
[assembly: AssemblyDescription("Paint.NET Plugin for OpenSim ORM Map Generation")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Manfred Aabye")]
[assembly: AssemblyProduct("ORM Generator")]
[assembly: AssemblyCopyright("Copyright © 2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: Guid("8d4a9b2c-1e8f-4a3d-b5c7-9d2e4f6a8b1c")]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
```

## **6. README.txt**

```markdown
# ORM Map Generator für Paint.NET

Ein Plugin zur Erstellung von ORM (Occlusion-Roughness-Metallic) Maps für OpenSim und andere PBR-Renderer.

## Installation:
1. Kompiliere das Projekt in Visual Studio
2. Kopiere die ORMGenerator.dll in den Paint.NET Effects Ordner:
   - `C:\Program Files\paint.net\Effects\`
3. Starte Paint.NET neu

## Funktionen:
- Automatische ORM-Erkennung aus Bildern
- Manuelle Einstellung aller Kanäle
- Verschiedene Quellen für jeden Kanal
- Invertierungsoptionen
- Preview-Modus

## Verwendung:
1. Öffne ein Bild in Paint.NET
2. Gehe zu: Effekte > Photo > ORM Map Generator
3. Stelle die Parameter ein
4. Klicke OK zum Anwenden

## ORM Map Format:
- R-Kanal: Roughness (0 = glatt, 255 = rau)
- G-Kanal: Metallic (0 = nicht metallisch, 255 = metallisch)
- B-Kanal: Ambient Occlusion (0 = verdeckt, 255 = sichtbar)
```

## **7. Kompilierung und Installation**

### **Schritte:**

1. **Visual Studio öffnen** und neues Projekt erstellen
2. **Dateien einfügen** wie oben gezeigt
3. **Referenzen anpassen** (Pfade zu Paint.NET DLLs)
4. **Kompilieren** (Release-Modus)
5. **DLL kopieren** in Paint.NET Effects-Ordner
6. **Paint.NET neustarten**

### **Wichtige Hinweise:**

- Ändere die GUID in AssemblyInfo.cs
- Passe die Paint.NET DLL-Pfade in der .csproj an
- Teste das Plugin gründlich

## **8. Erweiterungsmöglichkeiten**

Du kannst das Plugin erweitern um:

- **Batch-Verarbeitung** für mehrere Dateien
- **Presets** für häufig verwendete Materialien
- **Normal Map Integration**
- **Height Map Support**
- **Export in verschiedene Formate** (TGA, DDS)
