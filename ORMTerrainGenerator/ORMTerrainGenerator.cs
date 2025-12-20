using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace ORMTerrainGeneratorEffect
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author => "ManfredAabye";
        public string Copyright => "Copyright © 2025";
        public string DisplayName => "Terrain Heightmap Generator";
        public Version Version => new Version(1, 2, 0, 0);
        public Uri WebsiteUri => new Uri("https://github.com/ManfredAabye");
    }

    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public class ORMTerrainGenerator : PropertyBasedEffect
    {
        // Island shape types
        private enum IslandShape
        {
            Normal,              // Organische runde Form (Standard)
            Filled               // Gefüllte Form ohne Wasser
        }

        // Property names
        private enum PropertyNames
        {
            IslandShape,
            RandomSeed,
            IslandSize,
            MountainIntensity,
            ErosionIterations,
            NoiseScale,
            NoiseOctaves,
            WaterLevel,
            BeachWidth,
            CoastlineRoughness,
            MountainCount,
            ValleyDepth,
            LakeCount,
            TerrainType,
            Persistence
        }

        // Heightmap data
        private double[,]? heightmap;
        private int currentIslandShape;
        private int currentSeed;
        private double currentIslandSize;
        private double currentMountainIntensity;
        private int currentErosionIterations;
        private double currentNoiseScale;
        private int currentNoiseOctaves;
        private double currentWaterLevel;
        private double currentBeachWidth;
        private double currentCoastlineRoughness;
        private int currentMountainCount;
        private double currentValleyDepth;
        private int currentLakeCount;
        private int currentTerrainType;
        private double currentPersistence;

        // Constructor
        public ORMTerrainGenerator()
            : base("Terrain Heightmap Generator",
                   (System.Drawing.Image?)null,
                   "ORM",
                   new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        // Create property collection for UI
        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>
            {
                new StaticListChoiceProperty(PropertyNames.IslandShape, new object[] { IslandShape.Normal, IslandShape.Filled }, 0, false),
                new Int32Property(PropertyNames.RandomSeed, 0, 0, 99999),
                new DoubleProperty(PropertyNames.IslandSize, 0.4, 0.2, 0.8),
                new DoubleProperty(PropertyNames.MountainIntensity, 0.5, 0.0, 1.0),
                new Int32Property(PropertyNames.ErosionIterations, 3, 0, 10),
                new DoubleProperty(PropertyNames.NoiseScale, 4.0, 1.0, 10.0),
                new Int32Property(PropertyNames.NoiseOctaves, 4, 1, 8),
                new DoubleProperty(PropertyNames.WaterLevel, 40.0, 0.0, 100.0),
                new DoubleProperty(PropertyNames.BeachWidth, 0.1, 0.0, 0.3),
                new DoubleProperty(PropertyNames.CoastlineRoughness, 0.25, 0.0, 1.0),
                new Int32Property(PropertyNames.MountainCount, 5, 0, 20),
                new DoubleProperty(PropertyNames.ValleyDepth, 0.3, 0.0, 1.0),
                new Int32Property(PropertyNames.LakeCount, 5, 0, 20),
                new Int32Property(PropertyNames.TerrainType, 0, 0, 3),
                new DoubleProperty(PropertyNames.Persistence, 0.5, 0.2, 0.9)
            };

            return new PropertyCollection(props);
        }

        // Configure property controls
        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.IslandShape, ControlInfoPropertyNames.DisplayName, "Inselform");
            configUI.SetPropertyControlValue(PropertyNames.IslandShape, ControlInfoPropertyNames.Description, "Form der Insel: Normal oder Gefüllt");
            PropertyControlInfo islandShapeControl = configUI.FindControlForPropertyName(PropertyNames.IslandShape);
            islandShapeControl.SetValueDisplayName(IslandShape.Normal, "Normal");
            islandShapeControl.SetValueDisplayName(IslandShape.Filled, "Gefüllt");

            configUI.SetPropertyControlValue(PropertyNames.RandomSeed, ControlInfoPropertyNames.DisplayName, "Zufalls-Seed");
            configUI.SetPropertyControlValue(PropertyNames.RandomSeed, ControlInfoPropertyNames.Description, "Seed für Zufallsgenerierung (0 = zufällig)");

            configUI.SetPropertyControlValue(PropertyNames.IslandSize, ControlInfoPropertyNames.DisplayName, "Inselgröße");
            configUI.SetPropertyControlValue(PropertyNames.IslandSize, ControlInfoPropertyNames.Description, "Größe der generierten Insel");
            configUI.SetPropertyControlValue(PropertyNames.IslandSize, ControlInfoPropertyNames.SliderLargeChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.IslandSize, ControlInfoPropertyNames.SliderSmallChange, 0.01);
            configUI.SetPropertyControlValue(PropertyNames.IslandSize, ControlInfoPropertyNames.UpDownIncrement, 0.01);

            configUI.SetPropertyControlValue(PropertyNames.MountainIntensity, ControlInfoPropertyNames.DisplayName, "Berg-Intensität");
            configUI.SetPropertyControlValue(PropertyNames.MountainIntensity, ControlInfoPropertyNames.Description, "Wie stark ausgeprägt die Berge sind");
            configUI.SetPropertyControlValue(PropertyNames.MountainIntensity, ControlInfoPropertyNames.SliderLargeChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.MountainIntensity, ControlInfoPropertyNames.SliderSmallChange, 0.01);
            configUI.SetPropertyControlValue(PropertyNames.MountainIntensity, ControlInfoPropertyNames.UpDownIncrement, 0.01);

            configUI.SetPropertyControlValue(PropertyNames.ErosionIterations, ControlInfoPropertyNames.DisplayName, "Erosions-Iterationen");
            configUI.SetPropertyControlValue(PropertyNames.ErosionIterations, ControlInfoPropertyNames.Description, "Anzahl der Erosions-Durchgänge für sanftere Übergänge");

            configUI.SetPropertyControlValue(PropertyNames.NoiseScale, ControlInfoPropertyNames.DisplayName, "Rausch-Skalierung");
            configUI.SetPropertyControlValue(PropertyNames.NoiseScale, ControlInfoPropertyNames.Description, "Skalierung des Perlin-Noise (höher = feiner)");
            configUI.SetPropertyControlValue(PropertyNames.NoiseScale, ControlInfoPropertyNames.SliderLargeChange, 1.0);
            configUI.SetPropertyControlValue(PropertyNames.NoiseScale, ControlInfoPropertyNames.SliderSmallChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.NoiseScale, ControlInfoPropertyNames.UpDownIncrement, 0.1);

            configUI.SetPropertyControlValue(PropertyNames.NoiseOctaves, ControlInfoPropertyNames.DisplayName, "Rausch-Oktaven");
            configUI.SetPropertyControlValue(PropertyNames.NoiseOctaves, ControlInfoPropertyNames.Description, "Anzahl der Perlin-Noise Schichten");

            configUI.SetPropertyControlValue(PropertyNames.WaterLevel, ControlInfoPropertyNames.DisplayName, "Wasserhöhe (RGB)");
            configUI.SetPropertyControlValue(PropertyNames.WaterLevel, ControlInfoPropertyNames.Description, "RGB-Wert für Wasseroberfläche (Standard: 40 = 0m)");
            configUI.SetPropertyControlValue(PropertyNames.WaterLevel, ControlInfoPropertyNames.SliderLargeChange, 10.0);
            configUI.SetPropertyControlValue(PropertyNames.WaterLevel, ControlInfoPropertyNames.SliderSmallChange, 1.0);
            configUI.SetPropertyControlValue(PropertyNames.WaterLevel, ControlInfoPropertyNames.UpDownIncrement, 1.0);

            configUI.SetPropertyControlValue(PropertyNames.BeachWidth, ControlInfoPropertyNames.DisplayName, "Strandbreite");
            configUI.SetPropertyControlValue(PropertyNames.BeachWidth, ControlInfoPropertyNames.Description, "Breite des Strandbereichs an der Küste");
            configUI.SetPropertyControlValue(PropertyNames.BeachWidth, ControlInfoPropertyNames.SliderLargeChange, 0.05);
            configUI.SetPropertyControlValue(PropertyNames.BeachWidth, ControlInfoPropertyNames.SliderSmallChange, 0.01);
            configUI.SetPropertyControlValue(PropertyNames.BeachWidth, ControlInfoPropertyNames.UpDownIncrement, 0.01);

            configUI.SetPropertyControlValue(PropertyNames.CoastlineRoughness, ControlInfoPropertyNames.DisplayName, "Küsten-Rauheit");
            configUI.SetPropertyControlValue(PropertyNames.CoastlineRoughness, ControlInfoPropertyNames.Description, "Wie zerklüftet die Küstenlinie ist");
            configUI.SetPropertyControlValue(PropertyNames.CoastlineRoughness, ControlInfoPropertyNames.SliderLargeChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.CoastlineRoughness, ControlInfoPropertyNames.SliderSmallChange, 0.01);
            configUI.SetPropertyControlValue(PropertyNames.CoastlineRoughness, ControlInfoPropertyNames.UpDownIncrement, 0.01);

            configUI.SetPropertyControlValue(PropertyNames.MountainCount, ControlInfoPropertyNames.DisplayName, "Anzahl Berge");
            configUI.SetPropertyControlValue(PropertyNames.MountainCount, ControlInfoPropertyNames.Description, "Anzahl der Bergkuppen auf der Insel");

            configUI.SetPropertyControlValue(PropertyNames.ValleyDepth, ControlInfoPropertyNames.DisplayName, "Taltiefe");
            configUI.SetPropertyControlValue(PropertyNames.ValleyDepth, ControlInfoPropertyNames.Description, "Wie tief die Täler zwischen Bergen sind");
            configUI.SetPropertyControlValue(PropertyNames.ValleyDepth, ControlInfoPropertyNames.SliderLargeChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.ValleyDepth, ControlInfoPropertyNames.SliderSmallChange, 0.01);
            configUI.SetPropertyControlValue(PropertyNames.ValleyDepth, ControlInfoPropertyNames.UpDownIncrement, 0.01);

            configUI.SetPropertyControlValue(PropertyNames.LakeCount, ControlInfoPropertyNames.DisplayName, "Anzahl Seen");
            configUI.SetPropertyControlValue(PropertyNames.LakeCount, ControlInfoPropertyNames.Description, "Anzahl der Seen auf der Insel");

            configUI.SetPropertyControlValue(PropertyNames.TerrainType, ControlInfoPropertyNames.DisplayName, "Terrain-Typ");
            configUI.SetPropertyControlValue(PropertyNames.TerrainType, ControlInfoPropertyNames.Description, "0=Organisch, 1=Gebirgig, 2=Flach, 3=Archipel");
            configUI.SetPropertyControlType(PropertyNames.TerrainType, PropertyControlType.Slider);

            configUI.SetPropertyControlValue(PropertyNames.Persistence, ControlInfoPropertyNames.DisplayName, "Persistenz");
            configUI.SetPropertyControlValue(PropertyNames.Persistence, ControlInfoPropertyNames.Description, "Rauheit des Terrains (höher = rauer)");
            configUI.SetPropertyControlValue(PropertyNames.Persistence, ControlInfoPropertyNames.SliderLargeChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.Persistence, ControlInfoPropertyNames.SliderSmallChange, 0.01);
            configUI.SetPropertyControlValue(PropertyNames.Persistence, ControlInfoPropertyNames.UpDownIncrement, 0.01);

            return configUI;
        }

        // Main rendering function
        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            // Get parameters
            int islandShape = (int)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.IslandShape)!.Value;
            int seed = newToken.GetProperty<Int32Property>(PropertyNames.RandomSeed)!.Value;
            double islandSize = newToken.GetProperty<DoubleProperty>(PropertyNames.IslandSize)!.Value;
            double mountainIntensity = newToken.GetProperty<DoubleProperty>(PropertyNames.MountainIntensity)!.Value;
            int erosionIterations = newToken.GetProperty<Int32Property>(PropertyNames.ErosionIterations)!.Value;
            double noiseScale = newToken.GetProperty<DoubleProperty>(PropertyNames.NoiseScale)!.Value;
            int noiseOctaves = newToken.GetProperty<Int32Property>(PropertyNames.NoiseOctaves)!.Value;
            double waterLevel = newToken.GetProperty<DoubleProperty>(PropertyNames.WaterLevel)!.Value;
            double beachWidth = newToken.GetProperty<DoubleProperty>(PropertyNames.BeachWidth)!.Value;
            double coastlineRoughness = newToken.GetProperty<DoubleProperty>(PropertyNames.CoastlineRoughness)!.Value;
            int mountainCount = newToken.GetProperty<Int32Property>(PropertyNames.MountainCount)!.Value;
            double valleyDepth = newToken.GetProperty<DoubleProperty>(PropertyNames.ValleyDepth)!.Value;
            int lakeCount = newToken.GetProperty<Int32Property>(PropertyNames.LakeCount)!.Value;
            int terrainType = newToken.GetProperty<Int32Property>(PropertyNames.TerrainType)!.Value;
            double persistence = newToken.GetProperty<DoubleProperty>(PropertyNames.Persistence)!.Value;

            // Use random seed if 0
            if (seed == 0)
            {
                seed = new Random().Next(1, 99999);
            }

            // Check if any parameter has changed - if so, regenerate heightmap for real-time preview
            bool parametersChanged = heightmap == null ||
                                   currentIslandShape != islandShape ||
                                   currentSeed != seed ||
                                   Math.Abs(currentIslandSize - islandSize) > 0.001 ||
                                   Math.Abs(currentMountainIntensity - mountainIntensity) > 0.001 ||
                                   currentErosionIterations != erosionIterations ||
                                   Math.Abs(currentNoiseScale - noiseScale) > 0.001 ||
                                   currentNoiseOctaves != noiseOctaves ||
                                   Math.Abs(currentWaterLevel - waterLevel) > 0.001 ||
                                   Math.Abs(currentBeachWidth - beachWidth) > 0.001 ||
                                   Math.Abs(currentCoastlineRoughness - coastlineRoughness) > 0.001 ||
                                   currentMountainCount != mountainCount ||
                                   Math.Abs(currentValleyDepth - valleyDepth) > 0.001 ||
                                   currentLakeCount != lakeCount ||
                                   currentTerrainType != terrainType ||
                                   Math.Abs(currentPersistence - persistence) > 0.001;

            // Generate heightmap if parameters changed
            if (parametersChanged)
            {
                currentIslandShape = islandShape;
                currentSeed = seed;
                currentIslandSize = islandSize;
                currentMountainIntensity = mountainIntensity;
                currentErosionIterations = erosionIterations;
                currentNoiseScale = noiseScale;
                currentNoiseOctaves = noiseOctaves;
                currentWaterLevel = waterLevel;
                currentBeachWidth = beachWidth;
                currentCoastlineRoughness = coastlineRoughness;
                currentMountainCount = mountainCount;
                currentValleyDepth = valleyDepth;
                currentLakeCount = lakeCount;
                currentTerrainType = terrainType;
                currentPersistence = persistence;
                
                int size = Math.Min(dstArgs.Width, dstArgs.Height);
                heightmap = GenerateHeightmap(size, islandShape, seed, islandSize, mountainIntensity, erosionIterations, 
                                            noiseScale, noiseOctaves, waterLevel, beachWidth,
                                            coastlineRoughness, mountainCount, valleyDepth,
                                            lakeCount, terrainType, persistence);
            }

            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        // Render each region
        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            if (heightmap == null) return;

            int size = heightmap.GetLength(0);

            for (int i = startIndex; i < startIndex + length; i++)
            {
                Rectangle rect = renderRects[i];

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    if (IsCancelRequested) return;

                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        // Clamp to heightmap bounds
                        int hx = Math.Min(x, size - 1);
                        int hy = Math.Min(y, size - 1);

                        // Get height value and convert to color
                        double height = heightmap[hx, hy];
                        ColorBgra color = HeightToColor(height);

                        DstArgs.Surface[x, y] = color;
                    }
                }
            }
        }

        // Generate complete heightmap
        private double[,] GenerateHeightmap(int size, int islandShape, int seed, double islandSize, double mountainIntensity,
                                           int erosionIterations, double noiseScale, int noiseOctaves,
                                           double waterLevel, double beachWidth, double coastlineRoughness,
                                           int mountainCount, double valleyDepth,
                                           int lakeCount, int terrainType, double persistence)
        {
            double[,] hmap = new double[size, size];
            Random random = new Random(seed);

            // 1. Generate base heightmap with Perlin noise
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    double nx = (double)x / size * noiseScale;
                    double ny = (double)y / size * noiseScale;
                    hmap[x, y] = PerlinNoise(nx, ny, persistence, noiseOctaves, seed);
                }
            }

            // 2. Apply island shape (defines land/water boundaries)
            ApplyIslandShape(hmap, size, islandShape, islandSize, seed, terrainType, coastlineRoughness, beachWidth);

            // 3. Add mountains (large elevation features)
            if (mountainCount > 0 || mountainIntensity > 0)
            {
                AddMountains(hmap, size, mountainIntensity, noiseScale, noiseOctaves, seed, mountainCount, persistence);
            }

            // 4. Apply erosion first (smooths mountains and creates natural slopes)
            if (erosionIterations > 0)
            {
                ApplyErosion(hmap, size, erosionIterations);
            }

            // 5. Add valleys (after erosion, so they stay visible)
            if (valleyDepth > 0)
            {
                AddValleys(hmap, size, valleyDepth, noiseScale, noiseOctaves, seed, persistence);
            }

            // 6. Add lakes
            if (lakeCount > 0)
            {
                AddLakes(hmap, size, lakeCount, seed);
            }

            // 8. Final normalization (preserves details with threshold)
            NormalizeHeightmap(hmap, size);

            return hmap;
        }

        // Apply island shape with different geometric forms
        private void ApplyIslandShape(double[,] hmap, int size, int islandShape, double islandSize, int seed, int terrainType, double coastlineRoughness, double beachWidth)
        {
            Random random = new Random(seed + 1);
            int centerX = size / 2;
            int centerY = size / 2;
            double baseRadius = size * islandSize;

            // Generate Voronoi cells for bays and peninsulas (nur für Normal-Form)
            List<(int x, int y, bool isBay)> voronoiPoints = new List<(int x, int y, bool isBay)>();
            if (islandShape == (int)IslandShape.Normal)
            {
                voronoiPoints = GenerateVoronoiPoints(size, baseRadius, seed, 8);
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    double dx = x - centerX;
                    double dy = y - centerY;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    // Multi-scale Perlin noise for smooth organic coastline
                    double coastlineNoise = 0;
                    coastlineNoise += PerlinNoise(x * 0.01, y * 0.01, 0.5, 3, seed + 10) * 0.4 * coastlineRoughness;
                    coastlineNoise += PerlinNoise(x * 0.005, y * 0.005, 0.6, 2, seed + 11) * 0.3 * coastlineRoughness;
                    coastlineNoise += PerlinNoise(x * 0.02, y * 0.02, 0.4, 2, seed + 12) * 0.3 * coastlineRoughness;
                    
                    // Voronoi influence (nur für Normal-Form)
                    double voronoiInfluence = 0;
                    if (islandShape == (int)IslandShape.Normal)
                    {
                        voronoiInfluence = GetVoronoiInfluence(x, y, voronoiPoints);
                    }

                    // Berechne Distanz zur Form-Grenze basierend auf gewählter Form
                    double distanceToShape = CalculateDistanceToShape(x, y, centerX, centerY, size, baseRadius, islandShape, coastlineNoise, voronoiInfluence);
                    
                    // Für gefüllte Form: kein Wasser
                    if (islandShape == (int)IslandShape.Filled)
                    {
                        // Komplette Füllung - nur leichte Variation
                        double fillNoise = PerlinNoise(x * 0.005, y * 0.005, 0.5, 2, seed + 100);
                        hmap[x, y] += 0.3 + fillNoise * 0.1;
                    }
                    else if (distanceToShape > 0) // Außerhalb der Form (Wasser)
                    {
                        // Water (deep) - smoother transition
                        double waterDepth = distanceToShape / (size * 0.1);
                        hmap[x, y] = -0.8 - Math.Min(waterDepth * 0.5, 2.0);
                    }
                    else // Innerhalb der Form (Land)
                    {
                        // Island (rises toward center) - distanceToShape ist negativ, also invertieren
                        double normalizedDistance = Math.Abs(distanceToShape);
                        double maxDistance = GetMaxDistanceInShape(islandShape, size, baseRadius);
                        double factor = 1.0 - (normalizedDistance / maxDistance);
                        factor = Math.Max(0, Math.Min(1, factor));
                        
                        // Terrain type adjustment
                        double heightFactor = 0.7;
                        double powerFactor = 1.8;
                        if (terrainType == 1) // Mountainous: steeper
                        {
                            powerFactor = 1.3;
                            heightFactor = 0.9;
                        }
                        else if (terrainType == 2) // Flat: gentler
                        {
                            powerFactor = 2.5;
                            heightFactor = 0.5;
                        }
                        else if (terrainType == 3) // Archipelago: multiple islands
                        {
                            powerFactor = 1.5;
                            heightFactor = 0.6;
                            // Add archipelago pattern
                            double archipelagoNoise = PerlinNoise(x * 0.003, y * 0.003, 0.5, 3, seed + 50);
                            if (archipelagoNoise < 0.2) heightFactor *= 0.3; // Small islands
                        }
                        
                        factor = Math.Pow(factor, powerFactor);
                        hmap[x, y] += factor * heightFactor;

                        // Add beach (use beachWidth parameter) - only modify if in beach zone
                        if (beachWidth > 0 && distanceToShape > -50)
                        {
                            double beachFactor = (50 + distanceToShape) / 50.0;
                            beachFactor = Math.Max(0, Math.Min(1, beachFactor));
                            double coastalVariation = PerlinNoise(x * 0.05, y * 0.05, 0.6, 2, seed + 20);
                            // Blend instead of replace
                            hmap[x, y] = hmap[x, y] * (1.0 - beachFactor * 0.7 * beachWidth) + coastalVariation * 0.05 * beachFactor * beachWidth;
                        }
                    }
                }
            }

            // Apply smoothing pass specifically for coastal regions to remove artifacts
            SmoothCoastalRegions(hmap, size, baseRadius);
        }

        // Calculate distance to shape boundary (negative = inside, positive = outside)
        private double CalculateDistanceToShape(int x, int y, int centerX, int centerY, int size, double baseRadius, int islandShape, double coastlineNoise, double voronoiInfluence)
        {
            double dx = x - centerX;
            double dy = y - centerY;
            double absDx = Math.Abs(dx);
            double absDy = Math.Abs(dy);
            
            switch ((IslandShape)islandShape)
            {
                case IslandShape.Normal: // Organische runde Form
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    double effectiveRadius = baseRadius * (1.0 + coastlineNoise * 0.25 + voronoiInfluence);
                    return distance - effectiveRadius;
                
                case IslandShape.Filled: // Gefüllt
                    return -1000; // Immer innerhalb
                
                default:
                    return 0;
            }
        }

        // Get maximum distance within a shape (for normalization)
        private double GetMaxDistanceInShape(int islandShape, int size, double baseRadius)
        {
            switch ((IslandShape)islandShape)
            {
                case IslandShape.Normal:
                    return baseRadius;
                case IslandShape.Filled:
                    return size / 2;
                default:
                    return baseRadius;
            }
        }

        // Smooth coastal regions to remove radial artifacts
        private void SmoothCoastalRegions(double[,] hmap, int size, double baseRadius)
        {
            double[,] smoothed = new double[size, size];
            int centerX = size / 2;
            int centerY = size / 2;

            for (int y = 1; y < size - 1; y++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    double dx = x - centerX;
                    double dy = y - centerY;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    // Only smooth near the coastline
                    double coastalRange = baseRadius * 0.3;
                    double distFromCoast = Math.Abs(distance - baseRadius);

                    if (distFromCoast < coastalRange)
                    {
                        // 5x5 Gaussian-like smooth
                        double sum = 0;
                        double weight = 0;

                        for (int dy2 = -2; dy2 <= 2; dy2++)
                        {
                            for (int dx2 = -2; dx2 <= 2; dx2++)
                            {
                                int nx = x + dx2;
                                int ny = y + dy2;

                                if (nx >= 0 && nx < size && ny >= 0 && ny < size)
                                {
                                    double dist = Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                                    double w = Math.Exp(-dist * 0.5);
                                    sum += hmap[nx, ny] * w;
                                    weight += w;
                                }
                            }
                        }

                        double blendFactor = 1.0 - (distFromCoast / coastalRange);
                        smoothed[x, y] = hmap[x, y] * (1.0 - blendFactor * 0.6) + (sum / weight) * (blendFactor * 0.6);
                    }
                    else
                    {
                        smoothed[x, y] = hmap[x, y];
                    }
                }
            }

            // Copy back
            for (int y = 1; y < size - 1; y++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    hmap[x, y] = smoothed[x, y];
                }
            }
        }

        // Generate Voronoi points for bays and peninsulas
        private List<(int x, int y, bool isBay)> GenerateVoronoiPoints(int size, double baseRadius, int seed, int numPoints)
        {
            Random random = new Random(seed + 2);
            List<(int x, int y, bool isBay)> points = new List<(int x, int y, bool isBay)>();
            int centerX = size / 2;
            int centerY = size / 2;

            for (int i = 0; i < numPoints; i++)
            {
                double angle = (Math.PI * 2 / numPoints) * i + random.NextDouble() * 0.5;
                double radiusFactor = 0.8 + random.NextDouble() * 0.4;
                double radius = baseRadius * radiusFactor;
                
                int x = (int)(centerX + Math.Cos(angle) * radius);
                int y = (int)(centerY + Math.Sin(angle) * radius);
                bool isBay = random.NextDouble() > 0.5; // 50% chance for bay vs peninsula
                
                points.Add((x, y, isBay));
            }

            return points;
        }

        // Get Voronoi influence for creating bays and peninsulas
        private double GetVoronoiInfluence(int x, int y, List<(int px, int py, bool isBay)> points)
        {
            if (points.Count == 0) return 0;

            double minDist = double.MaxValue;
            double secondMinDist = double.MaxValue;
            bool nearestIsBay = false;

            // Find nearest and second nearest Voronoi points
            foreach (var point in points)
            {
                double dx = x - point.px;
                double dy = y - point.py;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                
                if (dist < minDist)
                {
                    secondMinDist = minDist;
                    minDist = dist;
                    nearestIsBay = point.isBay;
                }
                else if (dist < secondMinDist)
                {
                    secondMinDist = dist;
                }
            }

            // Use distance difference for smoother transitions at cell borders
            double edgeDistance = secondMinDist - minDist;
            double smoothFactor = Math.Tanh(edgeDistance * 0.01);
            
            // Influence decreases with distance
            double influence = Math.Exp(-minDist * 0.008) * smoothFactor;
            
            // Bays push inward (negative), peninsulas push outward (positive)
            return nearestIsBay ? -influence * 0.25 : influence * 0.2;
        }

        // Add mountains with multi-octave noise
        private void AddMountains(double[,] hmap, int size, double intensity, 
                                 double noiseScale, int octaves, int seed, int mountainCount, double persistence)
        {
            Random random = new Random(seed + 300);
            
            // Add specific mountain peaks
            for (int i = 0; i < mountainCount; i++)
            {
                int mx = random.Next(size / 4, size * 3 / 4);
                int my = random.Next(size / 4, size * 3 / 4);
                double mountainRadius = size * (0.1 + random.NextDouble() * 0.15);
                double mountainHeight = 0.4 + random.NextDouble() * 0.3;
                
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        if (hmap[x, y] > 0)
                        {
                            double dx = x - mx;
                            double dy = y - my;
                            double dist = Math.Sqrt(dx * dx + dy * dy);
                            if (dist < mountainRadius)
                            {
                                double factor = 1.0 - (dist / mountainRadius);
                                factor = Math.Pow(factor, 1.5);
                                hmap[x, y] += factor * mountainHeight * intensity;
                            }
                        }
                    }
                }
            }
            
            // Add general mountainous terrain with noise
            for (int octave = 0; octave < octaves; octave++)
            {
                double frequency = Math.Pow(2, octave);
                double amplitude = Math.Pow(persistence, octave);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        if (hmap[x, y] > 0) // Only on land
                        {
                            double nx = (double)x / size * noiseScale * 2 * frequency;
                            double ny = (double)y / size * noiseScale * 2 * frequency;
                            double noise = PerlinNoise(nx, ny, persistence, 4, seed + octave);
                            
                            // Only positive elevation for mountains
                            if (noise > 0)
                            {
                                hmap[x, y] += noise * amplitude * intensity * 0.4;
                            }
                        }
                    }
                }
            }
        }

        // Add valleys with multi-octave noise
        private void AddValleys(double[,] hmap, int size, double valleyDepth, 
                               double noiseScale, int octaves, int seed, double persistence)
        {
            // Add valleys using noise (only negative contributions)
            for (int octave = 0; octave < octaves; octave++)
            {
                double frequency = Math.Pow(2, octave);
                double amplitude = Math.Pow(persistence, octave);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        if (hmap[x, y] > 0.1) // Only on higher land (not beaches)
                        {
                            double nx = (double)x / size * noiseScale * 2 * frequency;
                            double ny = (double)y / size * noiseScale * 2 * frequency;
                            double noise = PerlinNoise(nx, ny, persistence, 4, seed + octave + 1000);
                            
                            // Only negative contribution for valleys
                            if (noise < 0)
                            {
                                hmap[x, y] += noise * amplitude * valleyDepth * 0.6;
                            }
                        }
                    }
                }
            }
        }

        // Add rivers
        // Add lakes
        private void AddLakes(double[,] hmap, int size, int lakeCount, int seed)
        {
            Random random = new Random(seed + 200);

            for (int i = 0; i < lakeCount; i++)
            {
                int lakeX = random.Next(size);
                int lakeY = random.Next(size);

                if (hmap[lakeX, lakeY] > 0.1 && hmap[lakeX, lakeY] < 0.4)
                {
                    // Larger lakes (10-25 radius instead of 5-15)
                    CreateLake(hmap, size, lakeX, lakeY, random.Next(10, 25));
                }
            }
        }

        // Create a lake
        private void CreateLake(double[,] hmap, int size, int centerX, int centerY, int radius)
        {
            double lakeHeight = 0.01; // Even lower (was 0.02)

            for (int y = Math.Max(0, centerY - radius); y < Math.Min(size, centerY + radius); y++)
            {
                for (int x = Math.Max(0, centerX - radius); x < Math.Min(size, centerX + radius); x++)
                {
                    double dx = x - centerX;
                    double dy = y - centerY;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance < radius && hmap[x, y] > 0)
                    {
                        double factor = 1.0 - (distance / radius);
                        // Deeper lakes (factor 0.9 instead of 0.8)
                        hmap[x, y] = Math.Max(lakeHeight, hmap[x, y] * (1.0 - factor * 0.9));
                    }
                }
            }
        }

        // Apply erosion for smooth transitions
        private void ApplyErosion(double[,] hmap, int size, int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                double[,] newHeightmap = new double[size, size];

                for (int y = 1; y < size - 1; y++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        // Apply erosion to all terrain (not just land)
                        double avg = (hmap[x - 1, y] + hmap[x + 1, y] + 
                                    hmap[x, y - 1] + hmap[x, y + 1]) / 4.0;
                        // Stronger erosion effect (0.5 instead of 0.7)
                        newHeightmap[x, y] = hmap[x, y] * 0.5 + avg * 0.5;
                    }
                }

                // Copy back
                for (int y = 1; y < size - 1; y++)
                {
                    for (int x = 1; x < size - 1; x++)
                    {
                        hmap[x, y] = newHeightmap[x, y];
                    }
                }
            }
        }

        // Normalize heightmap to full range
        private void NormalizeHeightmap(double[,] hmap, int size)
        {
            double min = double.MaxValue;
            double max = double.MinValue;

            // Find min/max
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    min = Math.Min(min, hmap[x, y]);
                    max = Math.Max(max, hmap[x, y]);
                }
            }

            // Only normalize if range is extreme (preserve details)
            double range = max - min;
            if (range > 3.0) // Only normalize if range is very large
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        hmap[x, y] = (hmap[x, y] - min) / range * 2.0 - 1.0;
                    }
                }
            }
        }

        // Convert height value to grayscale color (heightmap colors)
        private ColorBgra HeightToColor(double height)
        {
            // Heightmap colors in grayscale
            byte oceanColor = 0x14;    // #141414 - Ocean
            byte riverColor = 0x27;    // #272727 - Rivers and lakes
            byte valleyColor = 0x29;   // #292929 - Valleys
            byte mountainColor = 0x3C; // #3C3C3C - Mountains

            if (height < 0) // Ocean (#141414)
            {
                return ColorBgra.FromBgr(oceanColor, oceanColor, oceanColor);
            }
            else if (height < 0.05) // Rivers and lakes (#272727)
            {
                // Very low areas on island = rivers/lakes
                return ColorBgra.FromBgr(riverColor, riverColor, riverColor);
            }
            else if (height < 0.5) // Valleys with smooth transition
            {
                // From valleys (#292929) to mid height
                double factor = height / 0.5;
                int midValue = (valleyColor + mountainColor) / 2;
                byte gray = (byte)(valleyColor + (midValue - valleyColor) * factor);
                return ColorBgra.FromBgr(gray, gray, gray);
            }
            else // Mountains with smooth transition
            {
                // From mid height to mountains (#3C3C3C)
                double factor = (height - 0.5) / 0.5;
                int midValue = (valleyColor + mountainColor) / 2;
                byte gray = (byte)(midValue + (mountainColor - midValue) * factor);
                return ColorBgra.FromBgr(gray, gray, gray);
            }
        }

        // Perlin noise implementation
        private double PerlinNoise(double x, double y, double persistence, int octaves, int seed)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;
            double maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                total += InterpolatedNoise(x * frequency, y * frequency, seed + i) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }

        // Interpolated noise
        private double InterpolatedNoise(double x, double y, int seed)
        {
            int ix = (int)x;
            int iy = (int)y;
            double fx = x - ix;
            double fy = y - iy;

            double v1 = SmoothNoise(ix, iy, seed);
            double v2 = SmoothNoise(ix + 1, iy, seed);
            double v3 = SmoothNoise(ix, iy + 1, seed);
            double v4 = SmoothNoise(ix + 1, iy + 1, seed);

            double i1 = CosineInterpolate(v1, v2, fx);
            double i2 = CosineInterpolate(v3, v4, fx);

            return CosineInterpolate(i1, i2, fy);
        }

        // Deterministic smooth noise
        private double SmoothNoise(int x, int y, int seed)
        {
            // Hash function for deterministic noise
            int n = x + y * 57 + seed * 131;
            n = (n << 13) ^ n;
            return (1.0 - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0);
        }

        // Cosine interpolation
        private double CosineInterpolate(double a, double b, double x)
        {
            double ft = x * Math.PI;
            double f = (1 - Math.Cos(ft)) * 0.5;
            return a * (1 - f) + b * f;
        }
    }
}
