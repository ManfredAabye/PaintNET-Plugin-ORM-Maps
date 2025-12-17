using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace ORMHeightmapEffect
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author => "ManfredAabye";
        public string Copyright => "Copyright © 2025";
        public string DisplayName => "Heightmap Generator";
        public Version Version => new Version(1, 0, 0, 0);
        public Uri WebsiteUri => new Uri("https://github.com/ManfredAabye");
    }

    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public class ORMHeightmap : PropertyBasedEffect
    {
        // Property names
        private enum PropertyNames
        {
            BrushType,
            BrushSize,
            BrushStrength,
            TargetHeight,
            MaxTerrainHeight,
            Smoothness,
            AutoGenerate,
            AutoStyle,
            UE4Mode,
            ZScale,
            Use16Bit,
            MinGrayscale,
            MaxGrayscale,
            ClampRange
        }

        // Brush types
        private enum BrushType
        {
            // Basic Brushes
            Raise,
            Lower,
            Flatten,
            Smooth,
            
            // Mountain Brushes
            MountainPeak,
            MountainRidge,
            Hill,
            Plateau,
            
            // Canyon/Valley Brushes
            Canyon,
            Valley,
            River,
            
            // Building Brushes
            City,
            Buildings,
            Street,
            Road,
            
            // Water Brushes
            Lake,
            Ocean
        }

        // Auto-Generate Styles
        private enum AutoStyle
        {
            Mountains,
            Hills,
            Plains,
            Islands,
            Canyon,
            Dunes,
            Volcanic
        }

        // UE4 Recommended Landscape Sizes (from Epic's guide)
        private static readonly int[] UE4RecommendedSizes = new int[]
        {
            2017,  // Small
            4033,  // Medium
            8129   // Large
        };

        // Constructor
        public ORMHeightmap()
            : base("Heightmap Generator", 
                   (System.Drawing.Image?)null,
                   "ORM",
                   new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        // Property collection setup
        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>
            {
                new StaticListChoiceProperty(PropertyNames.BrushType, new object[] 
                {
                    BrushType.Raise,
                    BrushType.Lower,
                    BrushType.Flatten,
                    BrushType.Smooth,
                    BrushType.MountainPeak,
                    BrushType.MountainRidge,
                    BrushType.Hill,
                    BrushType.Plateau,
                    BrushType.Canyon,
                    BrushType.Valley,
                    BrushType.River,
                    BrushType.City,
                    BrushType.Buildings,
                    BrushType.Street,
                    BrushType.Road,
                    BrushType.Lake,
                    BrushType.Ocean
                }, 0),
                new Int32Property(PropertyNames.BrushSize, 100, 10, 500),
                new DoubleProperty(PropertyNames.BrushStrength, 1.0, 0.1, 5.0),
                new DoubleProperty(PropertyNames.TargetHeight, 10.0, -10.0, 30.0),
                new DoubleProperty(PropertyNames.MaxTerrainHeight, 500.0, 100.0, 10000.0),
                new DoubleProperty(PropertyNames.Smoothness, 0.5, 0.0, 1.0),
                new BooleanProperty(PropertyNames.AutoGenerate, false),
                new StaticListChoiceProperty(PropertyNames.AutoStyle, new object[]
                {
                    AutoStyle.Mountains,
                    AutoStyle.Hills,
                    AutoStyle.Plains,
                    AutoStyle.Islands,
                    AutoStyle.Canyon,
                    AutoStyle.Dunes,
                    AutoStyle.Volcanic
                }, 0),
                new BooleanProperty(PropertyNames.UE4Mode, false),
                new DoubleProperty(PropertyNames.ZScale, 100.0, 1.0, 500.0),
                new BooleanProperty(PropertyNames.Use16Bit, true),
                new Int32Property(PropertyNames.MinGrayscale, 30, 0, 255),
                new Int32Property(PropertyNames.MaxGrayscale, 60, 0, 255),
                new BooleanProperty(PropertyNames.ClampRange, false)
            };

            return new PropertyCollection(props);
        }

        // Control info setup
        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            // Brush Type
            configUI.SetPropertyControlValue(PropertyNames.BrushType, ControlInfoPropertyNames.DisplayName, "Heightmap Pinsel");
            PropertyControlInfo brushControl = configUI.FindControlForPropertyName(PropertyNames.BrushType);
            
            // Basic Brushes
            brushControl.SetValueDisplayName(BrushType.Raise, "Raise (Anheben)");
            brushControl.SetValueDisplayName(BrushType.Lower, "Lower (Absenken)");
            brushControl.SetValueDisplayName(BrushType.Flatten, "Flatten (Glätten)");
            brushControl.SetValueDisplayName(BrushType.Smooth, "Smooth (Weichzeichnen)");
            
            // Mountain Brushes
            brushControl.SetValueDisplayName(BrushType.MountainPeak, "Mountain Peak (Berggipfel)");
            brushControl.SetValueDisplayName(BrushType.MountainRidge, "Mountain Ridge (Bergrücken)");
            brushControl.SetValueDisplayName(BrushType.Hill, "Hill (Hügel)");
            brushControl.SetValueDisplayName(BrushType.Plateau, "Plateau (Hochebene)");
            
            // Canyon/Valley Brushes
            brushControl.SetValueDisplayName(BrushType.Canyon, "Canyon (Schlucht)");
            brushControl.SetValueDisplayName(BrushType.Valley, "Valley (Tal)");
            brushControl.SetValueDisplayName(BrushType.River, "River (Fluss)");
            
            // Building Brushes
            brushControl.SetValueDisplayName(BrushType.City, "City (Stadt)");
            brushControl.SetValueDisplayName(BrushType.Buildings, "Buildings (Gebäude)");
            brushControl.SetValueDisplayName(BrushType.Street, "Street (Straße)");
            brushControl.SetValueDisplayName(BrushType.Road, "Road (Landstraße)");
            
            // Water Brushes
            brushControl.SetValueDisplayName(BrushType.Lake, "Lake (See)");
            brushControl.SetValueDisplayName(BrushType.Ocean, "Ocean (Meer)");
            
            configUI.SetPropertyControlValue(PropertyNames.BrushType, ControlInfoPropertyNames.Description, "Wähle Heightmap-Pinsel");

            // Brush Size
            configUI.SetPropertyControlValue(PropertyNames.BrushSize, ControlInfoPropertyNames.DisplayName, "Pinselgröße");
            configUI.SetPropertyControlValue(PropertyNames.BrushSize, ControlInfoPropertyNames.Description, "Größe des Pinsels in Pixeln");

            // Brush Strength
            configUI.SetPropertyControlValue(PropertyNames.BrushStrength, ControlInfoPropertyNames.DisplayName, "Pinselstärke");
            configUI.SetPropertyControlValue(PropertyNames.BrushStrength, ControlInfoPropertyNames.SliderLargeChange, 0.5);
            configUI.SetPropertyControlValue(PropertyNames.BrushStrength, ControlInfoPropertyNames.SliderSmallChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.BrushStrength, ControlInfoPropertyNames.UpDownIncrement, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.BrushStrength, ControlInfoPropertyNames.DecimalPlaces, 1);
            configUI.SetPropertyControlValue(PropertyNames.BrushStrength, ControlInfoPropertyNames.Description, "Stärke des Pinseleffekts");

            // Target Height
            configUI.SetPropertyControlValue(PropertyNames.TargetHeight, ControlInfoPropertyNames.DisplayName, "Zielhöhe (Meter)");
            configUI.SetPropertyControlValue(PropertyNames.TargetHeight, ControlInfoPropertyNames.SliderLargeChange, 5.0);
            configUI.SetPropertyControlValue(PropertyNames.TargetHeight, ControlInfoPropertyNames.SliderSmallChange, 1.0);
            configUI.SetPropertyControlValue(PropertyNames.TargetHeight, ControlInfoPropertyNames.UpDownIncrement, 1.0);
            configUI.SetPropertyControlValue(PropertyNames.TargetHeight, ControlInfoPropertyNames.DecimalPlaces, 1);
            configUI.SetPropertyControlValue(PropertyNames.TargetHeight, ControlInfoPropertyNames.Description, "Zielhöhe in Metern (-10 bis 30m)");

            // Max Terrain Height
            configUI.SetPropertyControlValue(PropertyNames.MaxTerrainHeight, ControlInfoPropertyNames.DisplayName, "Max. Terrain-Höhe");
            configUI.SetPropertyControlValue(PropertyNames.MaxTerrainHeight, ControlInfoPropertyNames.SliderLargeChange, 100.0);
            configUI.SetPropertyControlValue(PropertyNames.MaxTerrainHeight, ControlInfoPropertyNames.SliderSmallChange, 50.0);
            configUI.SetPropertyControlValue(PropertyNames.MaxTerrainHeight, ControlInfoPropertyNames.UpDownIncrement, 50.0);
            configUI.SetPropertyControlValue(PropertyNames.MaxTerrainHeight, ControlInfoPropertyNames.DecimalPlaces, 0);
            configUI.SetPropertyControlValue(PropertyNames.MaxTerrainHeight, ControlInfoPropertyNames.Description, "Maximale Höhe des gesamten Terrains (Referenzwert für Grayscale-Berechnung)");

            // Smoothness
            configUI.SetPropertyControlValue(PropertyNames.Smoothness, ControlInfoPropertyNames.DisplayName, "Weichheit");
            configUI.SetPropertyControlValue(PropertyNames.Smoothness, ControlInfoPropertyNames.SliderLargeChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.Smoothness, ControlInfoPropertyNames.SliderSmallChange, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.Smoothness, ControlInfoPropertyNames.UpDownIncrement, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.Smoothness, ControlInfoPropertyNames.DecimalPlaces, 2);
            configUI.SetPropertyControlValue(PropertyNames.Smoothness, ControlInfoPropertyNames.Description, "Weichheit der Pinselkanten");

            // Auto-Generate
            configUI.SetPropertyControlValue(PropertyNames.AutoGenerate, ControlInfoPropertyNames.DisplayName, "Auto-Heightmap");
            configUI.SetPropertyControlValue(PropertyNames.AutoGenerate, ControlInfoPropertyNames.Description, "Automatische Heightmap-Generierung aktivieren");

            // Auto Style
            configUI.SetPropertyControlValue(PropertyNames.AutoStyle, ControlInfoPropertyNames.DisplayName, "Auto-Stil");
            PropertyControlInfo autoControl = configUI.FindControlForPropertyName(PropertyNames.AutoStyle);
            autoControl.SetValueDisplayName(AutoStyle.Mountains, "Mountains (Berge)");
            autoControl.SetValueDisplayName(AutoStyle.Hills, "Hills (Hügel)");
            autoControl.SetValueDisplayName(AutoStyle.Plains, "Plains (Ebenen)");
            autoControl.SetValueDisplayName(AutoStyle.Islands, "Islands (Inseln)");
            autoControl.SetValueDisplayName(AutoStyle.Canyon, "Canyon (Schlucht)");
            autoControl.SetValueDisplayName(AutoStyle.Dunes, "Dunes (Dünen)");
            autoControl.SetValueDisplayName(AutoStyle.Volcanic, "Volcanic (Vulkanisch)");
            configUI.SetPropertyControlValue(PropertyNames.AutoStyle, ControlInfoPropertyNames.Description, "Stil für automatische Generierung");

            // UE4 Mode
            configUI.SetPropertyControlValue(PropertyNames.UE4Mode, ControlInfoPropertyNames.DisplayName, "UE4-Modus");
            configUI.SetPropertyControlValue(PropertyNames.UE4Mode, ControlInfoPropertyNames.Description, "UE4-kompatible Heightmap (RGB 149 = Mittelpunkt, 16-bit)");

            // Z-Scale
            configUI.SetPropertyControlValue(PropertyNames.ZScale, ControlInfoPropertyNames.DisplayName, "Z-Scale");
            configUI.SetPropertyControlValue(PropertyNames.ZScale, ControlInfoPropertyNames.SliderLargeChange, 10.0);
            configUI.SetPropertyControlValue(PropertyNames.ZScale, ControlInfoPropertyNames.SliderSmallChange, 5.0);
            configUI.SetPropertyControlValue(PropertyNames.ZScale, ControlInfoPropertyNames.UpDownIncrement, 5.0);
            configUI.SetPropertyControlValue(PropertyNames.ZScale, ControlInfoPropertyNames.DecimalPlaces, 0);
            configUI.SetPropertyControlValue(PropertyNames.ZScale, ControlInfoPropertyNames.Description, "Höhen-Skalierung (UE4 Z-Achse)");

            // 16-bit Mode
            configUI.SetPropertyControlValue(PropertyNames.Use16Bit, ControlInfoPropertyNames.DisplayName, "16-bit Export");
            configUI.SetPropertyControlValue(PropertyNames.Use16Bit, ControlInfoPropertyNames.Description, "16-bit Grayscale für UE4/externe Software (empfohlen)");

            // Grayscale Range Clamping
            configUI.SetPropertyControlValue(PropertyNames.MinGrayscale, ControlInfoPropertyNames.DisplayName, "Min Grayscale");
            configUI.SetPropertyControlValue(PropertyNames.MinGrayscale, ControlInfoPropertyNames.Description, "Minimale Graustufe (Hex 1E = 30, tiefste Täler)");

            configUI.SetPropertyControlValue(PropertyNames.MaxGrayscale, ControlInfoPropertyNames.DisplayName, "Max Grayscale");
            configUI.SetPropertyControlValue(PropertyNames.MaxGrayscale, ControlInfoPropertyNames.Description, "Maximale Graustufe (Hex 3C = 60, höchste Berge)");

            configUI.SetPropertyControlValue(PropertyNames.ClampRange, ControlInfoPropertyNames.DisplayName, "Bereich begrenzen");
            configUI.SetPropertyControlValue(PropertyNames.ClampRange, ControlInfoPropertyNames.Description, "Graustufen auf Min/Max-Bereich begrenzen (Hex 1E-3C = RGB 30-60)");

            return configUI;
        }

        // Properties
        private BrushType brushType;
        private int brushSize;
        private double brushStrength;
        private double targetHeight;
        private double maxTerrainHeight;
        private double smoothness;
        private bool autoGenerate;
        private AutoStyle autoStyle;
        private bool ue4Mode;
        private double zScale;
        private bool use16Bit;
        private int minGrayscale;
        private int maxGrayscale;
        private bool clampRange;

        // Update properties from UI
        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            this.brushType = (BrushType)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.BrushType).Value;
            this.brushSize = newToken.GetProperty<Int32Property>(PropertyNames.BrushSize).Value;
            this.brushStrength = newToken.GetProperty<DoubleProperty>(PropertyNames.BrushStrength).Value;
            this.targetHeight = newToken.GetProperty<DoubleProperty>(PropertyNames.TargetHeight).Value;
            this.maxTerrainHeight = newToken.GetProperty<DoubleProperty>(PropertyNames.MaxTerrainHeight).Value;
            this.smoothness = newToken.GetProperty<DoubleProperty>(PropertyNames.Smoothness).Value;
            this.autoGenerate = newToken.GetProperty<BooleanProperty>(PropertyNames.AutoGenerate).Value;
            this.autoStyle = (AutoStyle)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.AutoStyle).Value;
            this.ue4Mode = newToken.GetProperty<BooleanProperty>(PropertyNames.UE4Mode).Value;
            this.zScale = newToken.GetProperty<DoubleProperty>(PropertyNames.ZScale).Value;
            this.use16Bit = newToken.GetProperty<BooleanProperty>(PropertyNames.Use16Bit).Value;
            this.minGrayscale = newToken.GetProperty<Int32Property>(PropertyNames.MinGrayscale).Value;
            this.maxGrayscale = newToken.GetProperty<Int32Property>(PropertyNames.MaxGrayscale).Value;
            this.clampRange = newToken.GetProperty<BooleanProperty>(PropertyNames.ClampRange).Value;

            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        // Main rendering function
        protected override void OnRender(Rectangle[] rois, int startIndex, int lengthParam)
        {
            if (lengthParam == 0) return;

            Surface dst = DstArgs.Surface;
            Surface src = SrcArgs.Surface;

            // If ClampRange is enabled, remap the entire grayscale range
            if (clampRange)
            {
                RemapGrayscaleRange(dst, src, rois, startIndex, lengthParam);
                return;
            }

            // Auto-Generate if enabled
            if (autoGenerate)
            {
                GenerateAutoHeightmap(dst, src, autoStyle);
                return;
            }

            // Apply brush to center of image
            int centerX = src.Width / 2;
            int centerY = src.Height / 2;

            for (int i = startIndex; i < startIndex + lengthParam; i++)
            {
                Rectangle rect = rois[i];

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        // Get source pixel
                        ColorBgra srcPixel = src[x, y];

                        // Calculate distance from center
                        float dx = x - centerX;
                        float dy = y - centerY;
                        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                        // Check if within brush radius
                        if (distance <= brushSize)
                        {
                            // Calculate brush influence (falloff from center)
                            float influence = 1.0f - (distance / brushSize);
                            influence = (float)Math.Pow(influence, 2.0 - smoothness); // Apply smoothness

                            // Get current height from grayscale
                            float currentHeight = GrayscaleToHeight(srcPixel, maxTerrainHeight);

                            // Apply brush effect
                            float newHeight = ApplyBrush(brushType, currentHeight, targetHeight, influence, brushStrength);

                            // Convert height back to grayscale
                            byte grayValue = HeightToGrayscaleNew(newHeight, maxTerrainHeight);

                            dst[x, y] = ColorBgra.FromBgra(grayValue, grayValue, grayValue, 255);
                        }
                        else
                        {
                            dst[x, y] = srcPixel;
                        }
                    }
                }
            }
        }

        // Remap entire grayscale range of image
        private void RemapGrayscaleRange(Surface dst, Surface src, Rectangle[] rois, int startIndex, int lengthParam)
        {
            // First pass: Find min and max grayscale values in source
            int srcMin = 255;
            int srcMax = 0;

            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    ColorBgra pixel = src[x, y];
                    int gray = (pixel.R + pixel.G + pixel.B) / 3;
                    srcMin = Math.Min(srcMin, gray);
                    srcMax = Math.Max(srcMax, gray);
                }
            }

            // Avoid division by zero
            if (srcMax == srcMin)
            {
                // Uniform color - set to middle of target range
                byte targetGray = (byte)((minGrayscale + maxGrayscale) / 2);
                for (int i = startIndex; i < startIndex + lengthParam; i++)
                {
                    Rectangle rect = rois[i];
                    for (int y = rect.Top; y < rect.Bottom; y++)
                    {
                        for (int x = rect.Left; x < rect.Right; x++)
                        {
                            dst[x, y] = ColorBgra.FromBgra(targetGray, targetGray, targetGray, 255);
                        }
                    }
                }
                return;
            }

            // Second pass: Remap values
            for (int i = startIndex; i < startIndex + lengthParam; i++)
            {
                Rectangle rect = rois[i];

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        ColorBgra srcPixel = src[x, y];
                        int srcGray = (srcPixel.R + srcPixel.G + srcPixel.B) / 3;

                        // Normalize to 0-1 range
                        float normalized = (float)(srcGray - srcMin) / (srcMax - srcMin);

                        // Remap to target range
                        byte targetGray = (byte)(minGrayscale + normalized * (maxGrayscale - minGrayscale));

                        dst[x, y] = ColorBgra.FromBgra(targetGray, targetGray, targetGray, 255);
                    }
                }
            }
        }

        // Apply brush effect based on type
        private float ApplyBrush(BrushType brush, float currentHeight, double targetHeightMeters, float influence, double strength)
        {
            float targetHeight = (float)targetHeightMeters;
            float effect = influence * (float)strength;

            switch (brush)
            {
                // Basic Brushes
                case BrushType.Raise:
                    return currentHeight + (targetHeight - currentHeight) * effect * 0.5f;

                case BrushType.Lower:
                    return currentHeight - (currentHeight - targetHeight) * effect * 0.5f;

                case BrushType.Flatten:
                    return currentHeight + (targetHeight - currentHeight) * effect;

                case BrushType.Smooth:
                    return currentHeight; // Smooth is handled differently

                // Mountain Brushes
                case BrushType.MountainPeak:
                    return currentHeight + targetHeight * influence * effect * (float)Math.Pow(influence, 0.5);

                case BrushType.MountainRidge:
                    float ridgePattern = (float)Math.Abs(Math.Sin(currentHeight * 0.1)) * 0.5f + 0.5f;
                    return currentHeight + targetHeight * influence * effect * ridgePattern;

                case BrushType.Hill:
                    return currentHeight + targetHeight * influence * effect * 0.7f;

                case BrushType.Plateau:
                    if (influence > 0.7f)
                        return targetHeight;
                    return currentHeight + (targetHeight - currentHeight) * influence * effect;

                // Canyon/Valley Brushes
                case BrushType.Canyon:
                    float canyonDepth = targetHeight * influence * effect;
                    return currentHeight - canyonDepth * (float)Math.Pow(influence, 2.0);

                case BrushType.Valley:
                    return currentHeight - targetHeight * influence * effect * 0.5f;

                case BrushType.River:
                    if (influence > 0.6f)
                        return -2.0f; // River bed
                    return currentHeight - (currentHeight + 2.0f) * influence * effect;

                // Building Brushes
                case BrushType.City:
                    // Blocky pattern for buildings
                    int gridX = (int)(currentHeight * 10) % 3;
                    int gridY = (int)(influence * 10) % 3;
                    if (gridX == 1 && gridY == 1)
                        return targetHeight * 0.5f;
                    return targetHeight * 0.1f;

                case BrushType.Buildings:
                    int buildingPattern = ((int)(currentHeight * 20) % 5);
                    if (buildingPattern == 0)
                        return targetHeight * 0.4f;
                    return targetHeight * 0.1f;

                case BrushType.Street:
                case BrushType.Road:
                    return 1.0f; // Flat at 1m above waterline

                // Water Brushes
                case BrushType.Lake:
                    return 0.0f; // Water level

                case BrushType.Ocean:
                    return -1.0f; // Below water level

                default:
                    return currentHeight;
            }
        }

        // Generate auto-heightmap based on style
        private void GenerateAutoHeightmap(Surface dst, Surface src, AutoStyle style)
        {
            int width = src.Width;
            int height = src.Height;
            Random rand = new Random(12345);

            // Generate noise-based heightmap
            float[,] noise = GeneratePerlinNoise(width, height, rand, GetOctavesForStyle(style));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noiseValue = noise[x, y];
                    float terrainHeight = ApplyStyleTransform(noiseValue, style, x, y, width, height);

                    byte grayValue = HeightToGrayscaleNew(terrainHeight, maxTerrainHeight);
                    dst[x, y] = ColorBgra.FromBgra(grayValue, grayValue, grayValue, 255);
                }
            }
        }

        // Get octaves for noise generation based on style
        private int GetOctavesForStyle(AutoStyle style)
        {
            switch (style)
            {
                case AutoStyle.Mountains: return 6;
                case AutoStyle.Hills: return 4;
                case AutoStyle.Plains: return 2;
                case AutoStyle.Islands: return 5;
                case AutoStyle.Canyon: return 5;
                case AutoStyle.Dunes: return 3;
                case AutoStyle.Volcanic: return 7;
                default: return 4;
            }
        }

        // Apply style-specific transformation
        private float ApplyStyleTransform(float noiseValue, AutoStyle style, int x, int y, int width, int height)
        {
            float centerX = width / 2.0f;
            float centerY = height / 2.0f;
            float distFromCenter = (float)Math.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
            float maxDist = (float)Math.Sqrt(centerX * centerX + centerY * centerY);
            float radialFactor = 1.0f - (distFromCenter / maxDist);

            switch (style)
            {
                case AutoStyle.Mountains:
                    return noiseValue * 30.0f * (float)Math.Pow(noiseValue, 1.5);

                case AutoStyle.Hills:
                    return noiseValue * 15.0f * (float)Math.Pow(noiseValue, 1.2);

                case AutoStyle.Plains:
                    return noiseValue * 3.0f + 1.0f;

                case AutoStyle.Islands:
                    // Island with ocean around edges
                    float islandHeight = noiseValue * 20.0f * radialFactor;
                    return Math.Max(-2.0f, islandHeight);

                case AutoStyle.Canyon:
                    // Deep valleys
                    return (noiseValue - 0.5f) * 40.0f;

                case AutoStyle.Dunes:
                    // Rolling sand dunes
                    return (float)Math.Sin(noiseValue * Math.PI * 4) * 8.0f + 2.0f;

                case AutoStyle.Volcanic:
                    // Volcanic crater with high peaks
                    if (radialFactor < 0.3f)
                        return -5.0f; // Crater
                    return noiseValue * 35.0f * (1.0f - radialFactor);

                default:
                    return noiseValue * 10.0f;
            }
        }

        // Generate Perlin noise
        private float[,] GeneratePerlinNoise(int width, int height, Random rand, int octaves)
        {
            float[,] noise = new float[width, height];
            float[,] baseNoise = new float[width, height];

            // Generate base random noise
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    baseNoise[x, y] = (float)rand.NextDouble();

            // Generate octaves
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float amplitude = 1.0f;
                    float frequency = 1.0f;
                    float noiseValue = 0.0f;

                    for (int octave = 0; octave < octaves; octave++)
                    {
                        int sampleX = (int)((x * frequency) % width);
                        int sampleY = (int)((y * frequency) % height);

                        noiseValue += baseNoise[sampleX, sampleY] * amplitude;

                        amplitude *= 0.5f;
                        frequency *= 2.0f;
                    }

                    noise[x, y] = noiseValue / octaves;
                }
            }

            return noise;
        }

        // Convert grayscale to height
        private float GrayscaleToHeight(ColorBgra pixel, double maxHeight)
        {
            int gray = (pixel.R + pixel.G + pixel.B) / 3;
            
            if (ue4Mode)
            {
                // UE4 Mode: RGB 149 is middle (0 height)
                float normalized;
                if (gray < 149)
                {
                    // Below middle: map 0-149 to 0.0-0.5
                    normalized = (gray / 149.0f) * 0.5f;
                }
                else
                {
                    // Above middle: map 149-255 to 0.5-1.0
                    normalized = 0.5f + ((gray - 149.0f) / (255.0f - 149.0f)) * 0.5f;
                }
                
                float height = (float)((normalized - 0.5) * maxHeight);
                return height / (float)(zScale / 100.0); // Reverse Z-Scale
            }
            else
            {
                // Standard Mode: Grayscale 0 = -maxHeight/2, 128 = 0, 255 = maxHeight/2
                float normalizedGray = gray / 255.0f;
                float height = (float)((normalizedGray - 0.5) * maxHeight);
                return height / (float)(zScale / 100.0); // Reverse Z-Scale
            }
        }

        // Convert height to grayscale (new version with max terrain height)
        private byte HeightToGrayscaleNew(float heightMeters, double maxHeight)
        {
            // Apply Z-Scale
            float scaledHeight = heightMeters * (float)(zScale / 100.0);
            
            if (ue4Mode)
            {
                // UE4 Mode: RGB 149 (#959595) is the middle point
                // Black (0) = lowest, RGB 149 = middle, White (255) = highest
                float normalized = (scaledHeight / (float)maxHeight) + 0.5f;
                normalized = Math.Max(0.0f, Math.Min(1.0f, normalized));
                
                // Map to 0-255 with 149 as center
                if (normalized < 0.5f)
                {
                    // Below middle: 0 to 149
                    return (byte)(normalized * 2.0f * 149.0f);
                }
                else
                {
                    // Above middle: 149 to 255
                    return (byte)(149.0f + (normalized - 0.5f) * 2.0f * (255.0f - 149.0f));
                }
            }
            else
            {
                // Standard Mode: 0 -> 0, 0.5 -> 128, 1.0 -> 255
                float normalized = (scaledHeight / (float)maxHeight) + 0.5f;
                normalized = Math.Max(0.0f, Math.Min(1.0f, normalized));
                
                return (byte)(normalized * 255.0f);
            }
        }


    }
}
