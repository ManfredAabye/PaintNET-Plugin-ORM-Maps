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
        public Version Version => new Version(1, 0, 0, 0);
        public Uri WebsiteUri => new Uri("https://github.com/ManfredAabye");
    }

    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public class ORMTerrainGenerator : PropertyBasedEffect
    {
        // Property names
        private enum PropertyNames
        {
            RandomSeed,
            IslandSize,
            MountainIntensity,
            ErosionIterations,
            RiverDensity,
            NoiseScale,
            NoiseOctaves
        }

        // Heightmap data
        private double[,]? heightmap;
        private int currentSeed;

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
                new Int32Property(PropertyNames.RandomSeed, 0, 0, 99999),
                new DoubleProperty(PropertyNames.IslandSize, 0.4, 0.2, 0.8),
                new DoubleProperty(PropertyNames.MountainIntensity, 0.5, 0.0, 1.0),
                new Int32Property(PropertyNames.ErosionIterations, 3, 0, 10),
                new DoubleProperty(PropertyNames.RiverDensity, 0.3, 0.0, 1.0),
                new DoubleProperty(PropertyNames.NoiseScale, 4.0, 1.0, 10.0),
                new Int32Property(PropertyNames.NoiseOctaves, 4, 1, 8)
            };

            return new PropertyCollection(props);
        }

        // Configure property controls
        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

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

            configUI.SetPropertyControlValue(PropertyNames.RiverDensity, ControlInfoPropertyNames.DisplayName, "Fluss-Dichte");
            configUI.SetPropertyControlValue(PropertyNames.RiverDensity, ControlInfoPropertyNames.Description, "Häufigkeit von Flüssen und Seen");
            configUI.SetPropertyControlValue(PropertyNames.RiverDensity, ControlInfoPropertyNames.SliderLargeChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.RiverDensity, ControlInfoPropertyNames.SliderSmallChange, 0.01);
            configUI.SetPropertyControlValue(PropertyNames.RiverDensity, ControlInfoPropertyNames.UpDownIncrement, 0.01);

            configUI.SetPropertyControlValue(PropertyNames.NoiseScale, ControlInfoPropertyNames.DisplayName, "Rausch-Skalierung");
            configUI.SetPropertyControlValue(PropertyNames.NoiseScale, ControlInfoPropertyNames.Description, "Skalierung des Perlin-Noise (höher = feiner)");
            configUI.SetPropertyControlValue(PropertyNames.NoiseScale, ControlInfoPropertyNames.SliderLargeChange, 1.0);
            configUI.SetPropertyControlValue(PropertyNames.NoiseScale, ControlInfoPropertyNames.SliderSmallChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.NoiseScale, ControlInfoPropertyNames.UpDownIncrement, 0.1);

            configUI.SetPropertyControlValue(PropertyNames.NoiseOctaves, ControlInfoPropertyNames.DisplayName, "Rausch-Oktaven");
            configUI.SetPropertyControlValue(PropertyNames.NoiseOctaves, ControlInfoPropertyNames.Description, "Anzahl der Perlin-Noise Schichten");

            return configUI;
        }

        // Main rendering function
        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            // Get parameters
            int seed = newToken.GetProperty<Int32Property>(PropertyNames.RandomSeed)!.Value;
            double islandSize = newToken.GetProperty<DoubleProperty>(PropertyNames.IslandSize)!.Value;
            double mountainIntensity = newToken.GetProperty<DoubleProperty>(PropertyNames.MountainIntensity)!.Value;
            int erosionIterations = newToken.GetProperty<Int32Property>(PropertyNames.ErosionIterations)!.Value;
            double riverDensity = newToken.GetProperty<DoubleProperty>(PropertyNames.RiverDensity)!.Value;
            double noiseScale = newToken.GetProperty<DoubleProperty>(PropertyNames.NoiseScale)!.Value;
            int noiseOctaves = newToken.GetProperty<Int32Property>(PropertyNames.NoiseOctaves)!.Value;

            // Use random seed if 0
            if (seed == 0)
            {
                seed = new Random().Next(1, 99999);
            }

            // Generate heightmap if needed
            if (heightmap == null || currentSeed != seed)
            {
                currentSeed = seed;
                int size = Math.Min(dstArgs.Width, dstArgs.Height);
                heightmap = GenerateHeightmap(size, seed, islandSize, mountainIntensity, erosionIterations, 
                                            riverDensity, noiseScale, noiseOctaves);
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
        private double[,] GenerateHeightmap(int size, int seed, double islandSize, double mountainIntensity,
                                           int erosionIterations, double riverDensity, double noiseScale, int noiseOctaves)
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
                    hmap[x, y] = PerlinNoise(nx, ny, 0.5, noiseOctaves, seed);
                }
            }

            // 2. Apply island shape
            ApplyIslandShape(hmap, size, islandSize, seed);

            // 3. Add mountains and valleys
            AddMountainsAndValleys(hmap, size, mountainIntensity, noiseScale, noiseOctaves, seed);

            // 4. Add rivers and lakes
            if (riverDensity > 0)
            {
                AddRiversAndLakes(hmap, size, riverDensity, seed);
            }

            // 5. Apply erosion for smooth transitions
            if (erosionIterations > 0)
            {
                ApplyErosion(hmap, size, erosionIterations);
            }

            // 6. Normalize heightmap
            NormalizeHeightmap(hmap, size);

            return hmap;
        }

        // Apply organic island shape with Perlin noise, Voronoi and Random Walk
        private void ApplyIslandShape(double[,] hmap, int size, double islandSize, int seed)
        {
            Random random = new Random(seed + 1);
            int centerX = size / 2;
            int centerY = size / 2;
            double baseRadius = size * islandSize;

            // Generate Voronoi cells for bays and peninsulas
            List<(int x, int y, bool isBay)> voronoiPoints = GenerateVoronoiPoints(size, baseRadius, seed, 8);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    double dx = x - centerX;
                    double dy = y - centerY;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    // Multi-scale Perlin noise for smooth organic coastline
                    double coastlineNoise = 0;
                    coastlineNoise += PerlinNoise(x * 0.01, y * 0.01, 0.5, 3, seed + 10) * 0.4;
                    coastlineNoise += PerlinNoise(x * 0.005, y * 0.005, 0.6, 2, seed + 11) * 0.3;
                    coastlineNoise += PerlinNoise(x * 0.02, y * 0.02, 0.4, 2, seed + 12) * 0.3;
                    
                    // Voronoi influence for bays and peninsulas
                    double voronoiInfluence = GetVoronoiInfluence(x, y, voronoiPoints);
                    
                    // Combine influences for organic shape (removed random walk to avoid radial patterns)
                    double effectiveRadius = baseRadius * (1.0 + coastlineNoise * 0.25 + voronoiInfluence);

                    // Island shape: falling edges
                    if (distance > effectiveRadius)
                    {
                        // Water (deep) - smoother transition
                        double waterDepth = (distance - effectiveRadius) / (size * 0.1);
                        hmap[x, y] = -0.8 - Math.Min(waterDepth * 0.5, 2.0);
                    }
                    else
                    {
                        // Island (rises toward center)
                        double factor = 1.0 - (distance / effectiveRadius);
                        // Use power function for more natural elevation distribution
                        factor = Math.Pow(factor, 1.8);
                        hmap[x, y] += factor * 0.7;

                        // Add subtle coastal variation using 2D noise (not angle-based)
                        if (distance > effectiveRadius * 0.75)
                        {
                            double coastalVariation = PerlinNoise(x * 0.05, y * 0.05, 0.6, 2, seed + 20);
                            double blendFactor = (distance - effectiveRadius * 0.75) / (effectiveRadius * 0.25);
                            hmap[x, y] += coastalVariation * 0.1 * (1.0 - blendFactor);
                        }
                    }
                }
            }

            // Apply smoothing pass specifically for coastal regions to remove artifacts
            SmoothCoastalRegions(hmap, size, baseRadius);
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

        // Add mountains and valleys with multi-octave noise
        private void AddMountainsAndValleys(double[,] hmap, int size, double intensity, 
                                           double noiseScale, int octaves, int seed)
        {
            for (int octave = 0; octave < octaves; octave++)
            {
                double frequency = Math.Pow(2, octave);
                double amplitude = Math.Pow(0.5, octave);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        if (hmap[x, y] > 0) // Only on land
                        {
                            double nx = (double)x / size * noiseScale * 2 * frequency;
                            double ny = (double)y / size * noiseScale * 2 * frequency;
                            double noise = PerlinNoise(nx, ny, 0.8, 4, seed + octave);
                            hmap[x, y] += noise * amplitude * intensity * 0.4;
                        }
                    }
                }
            }
        }

        // Add rivers and lakes
        private void AddRiversAndLakes(double[,] hmap, int size, double density, int seed)
        {
            Random random = new Random(seed + 100);
            int numRivers = (int)(density * 10);

            for (int i = 0; i < numRivers; i++)
            {
                // Random starting point on land
                int startX = random.Next(size);
                int startY = random.Next(size);

                if (hmap[startX, startY] > 0.2)
                {
                    CarveRiver(hmap, size, startX, startY, random);
                }
            }

            // Add some lakes
            int numLakes = (int)(density * 5);
            for (int i = 0; i < numLakes; i++)
            {
                int lakeX = random.Next(size);
                int lakeY = random.Next(size);

                if (hmap[lakeX, lakeY] > 0.1 && hmap[lakeX, lakeY] < 0.4)
                {
                    CreateLake(hmap, size, lakeX, lakeY, random.Next(5, 15));
                }
            }
        }

        // Carve a river following the terrain
        private void CarveRiver(double[,] hmap, int size, int startX, int startY, Random random)
        {
            int x = startX;
            int y = startY;
            int maxSteps = size;
            double riverDepth = 0.05;

            for (int step = 0; step < maxSteps; step++)
            {
                if (x < 1 || x >= size - 1 || y < 1 || y >= size - 1) break;
                if (hmap[x, y] <= 0) break; // Reached water

                // Lower current position
                hmap[x, y] = Math.Max(0.02, hmap[x, y] - riverDepth);

                // Find lowest neighbor
                int bestX = x;
                int bestY = y;
                double lowestHeight = hmap[x, y];

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < size && ny >= 0 && ny < size)
                        {
                            if (hmap[nx, ny] < lowestHeight)
                            {
                                lowestHeight = hmap[nx, ny];
                                bestX = nx;
                                bestY = ny;
                            }
                        }
                    }
                }

                // Move to lowest point or random direction
                if (bestX == x && bestY == y)
                {
                    // No lower point found, move randomly
                    x += random.Next(-1, 2);
                    y += random.Next(-1, 2);
                }
                else
                {
                    x = bestX;
                    y = bestY;
                }
            }
        }

        // Create a lake
        private void CreateLake(double[,] hmap, int size, int centerX, int centerY, int radius)
        {
            double lakeHeight = 0.02;

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
                        hmap[x, y] = Math.Max(lakeHeight, hmap[x, y] * (1.0 - factor * 0.8));
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
                        if (hmap[x, y] > 0) // Only erode land
                        {
                            double avg = (hmap[x - 1, y] + hmap[x + 1, y] + 
                                        hmap[x, y - 1] + hmap[x, y + 1]) / 4.0;
                            newHeightmap[x, y] = hmap[x, y] * 0.7 + avg * 0.3;
                        }
                        else
                        {
                            newHeightmap[x, y] = hmap[x, y];
                        }
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

            // Normalize to -1 to 1 range
            double range = max - min;
            if (range > 0)
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
