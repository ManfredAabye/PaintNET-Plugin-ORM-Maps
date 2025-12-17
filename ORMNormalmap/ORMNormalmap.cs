using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace ORMNormalmapEffect
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author => "ManfredAabye";
        public string Copyright => "Copyright © 2025";
        public string DisplayName => "Normal Map Generator";
        public Version Version => new Version(1, 0, 0, 0);
        public Uri WebsiteUri => new Uri("https://github.com/ManfredAabye");
    }

    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public class ORMNormalmap : PropertyBasedEffect
    {
        // Property names
        private enum PropertyNames
        {
            Strength,
            Depth,
            Invert,
            BlurRadius
        }

        // Constructor
        public ORMNormalmap()
            : base("Normal Map Generator", 
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
                new DoubleProperty(PropertyNames.Strength, 5.0, 0.1, 20.0),
                new DoubleProperty(PropertyNames.Depth, 1.0, 0.1, 5.0),
                new BooleanProperty(PropertyNames.Invert, false),
                new Int32Property(PropertyNames.BlurRadius, 0, 0, 5)
            };

            return new PropertyCollection(props);
        }

        // Control info setup
        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.Strength, ControlInfoPropertyNames.DisplayName, "Strength");
            configUI.SetPropertyControlValue(PropertyNames.Strength, ControlInfoPropertyNames.SliderLargeChange, 1.0);
            configUI.SetPropertyControlValue(PropertyNames.Strength, ControlInfoPropertyNames.SliderSmallChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.Strength, ControlInfoPropertyNames.UpDownIncrement, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.Strength, ControlInfoPropertyNames.DecimalPlaces, 1);
            configUI.SetPropertyControlValue(PropertyNames.Strength, ControlInfoPropertyNames.Description, "Stärke der Normal Map (Emboss)");

            configUI.SetPropertyControlValue(PropertyNames.Depth, ControlInfoPropertyNames.DisplayName, "Depth");
            configUI.SetPropertyControlValue(PropertyNames.Depth, ControlInfoPropertyNames.SliderLargeChange, 0.5);
            configUI.SetPropertyControlValue(PropertyNames.Depth, ControlInfoPropertyNames.SliderSmallChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.Depth, ControlInfoPropertyNames.UpDownIncrement, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.Depth, ControlInfoPropertyNames.DecimalPlaces, 1);
            configUI.SetPropertyControlValue(PropertyNames.Depth, ControlInfoPropertyNames.Description, "Tiefe der Normal Map");

            configUI.SetPropertyControlValue(PropertyNames.Invert, ControlInfoPropertyNames.DisplayName, "Invert");
            configUI.SetPropertyControlValue(PropertyNames.Invert, ControlInfoPropertyNames.Description, "Normale umkehren");

            configUI.SetPropertyControlValue(PropertyNames.BlurRadius, ControlInfoPropertyNames.DisplayName, "Blur Radius");
            configUI.SetPropertyControlValue(PropertyNames.BlurRadius, ControlInfoPropertyNames.Description, "Weichzeichnung vor Verarbeitung (0 = aus)");

            return configUI;
        }

        // Properties
        private double strength;
        private double depth;
        private bool invert;
        private int blurRadius;

        // Update properties from UI
        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            this.strength = newToken.GetProperty<DoubleProperty>(PropertyNames.Strength).Value;
            this.depth = newToken.GetProperty<DoubleProperty>(PropertyNames.Depth).Value;
            this.invert = newToken.GetProperty<BooleanProperty>(PropertyNames.Invert).Value;
            this.blurRadius = newToken.GetProperty<Int32Property>(PropertyNames.BlurRadius).Value;

            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        // Main rendering function
        protected override void OnRender(Rectangle[] rois, int startIndex, int length)
        {
            if (length == 0) return;

            Surface dst = DstArgs.Surface;
            Surface src = SrcArgs.Surface;

            // Create grayscale buffer for the entire surface
            byte[,] grayscale = new byte[src.Width, src.Height];

            // First pass: Convert to grayscale with optional blur
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    ColorBgra pixel = src[x, y];
                    int gray = (pixel.R + pixel.G + pixel.B) / 3;
                    grayscale[x, y] = (byte)gray;
                }
            }

            // Apply blur if needed
            if (blurRadius > 0)
            {
                grayscale = ApplyGaussianBlur(grayscale, src.Width, src.Height, blurRadius);
            }

            // Second pass: Generate normal map using Sobel operator
            for (int i = startIndex; i < startIndex + length; i++)
            {
                Rectangle rect = rois[i];

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        // Sobel operator for gradient detection
                        float gx = CalculateSobelX(grayscale, x, y, src.Width, src.Height);
                        float gy = CalculateSobelY(grayscale, x, y, src.Width, src.Height);

                        // Apply strength and depth
                        gx *= (float)strength * (float)depth;
                        gy *= (float)strength * (float)depth;

                        // Invert if needed
                        if (invert)
                        {
                            gx = -gx;
                            gy = -gy;
                        }

                        // Calculate Z component (perpendicular to surface)
                        float gz = 1.0f;

                        // Normalize the normal vector
                        float vecLength = (float)Math.Sqrt(gx * gx + gy * gy + gz * gz);
                        if (vecLength > 0.0001f)
                        {
                            gx /= vecLength;
                            gy /= vecLength;
                            gz /= vecLength;
                        }

                        // Convert from [-1,1] to [0,255] range
                        byte normalX = (byte)((gx * 0.5f + 0.5f) * 255.0f);
                        byte normalY = (byte)((gy * 0.5f + 0.5f) * 255.0f);
                        byte normalZ = (byte)((gz * 0.5f + 0.5f) * 255.0f);

                        // Normal maps: R=X, G=Y, B=Z
                        dst[x, y] = ColorBgra.FromBgra(normalZ, normalY, normalX, 255);
                    }
                }
            }
        }

        // Sobel operator for X gradient
        private float CalculateSobelX(byte[,] grayscale, int x, int y, int width, int height)
        {
            // Sobel X kernel:
            // -1  0  1
            // -2  0  2
            // -1  0  1

            float sum = 0.0f;

            // Top row
            sum += GetGrayscaleSafe(grayscale, x - 1, y - 1, width, height) * -1.0f;
            sum += GetGrayscaleSafe(grayscale, x + 1, y - 1, width, height) * 1.0f;

            // Middle row
            sum += GetGrayscaleSafe(grayscale, x - 1, y, width, height) * -2.0f;
            sum += GetGrayscaleSafe(grayscale, x + 1, y, width, height) * 2.0f;

            // Bottom row
            sum += GetGrayscaleSafe(grayscale, x - 1, y + 1, width, height) * -1.0f;
            sum += GetGrayscaleSafe(grayscale, x + 1, y + 1, width, height) * 1.0f;

            return sum / 255.0f; // Normalize to [-1, 1] range
        }

        // Sobel operator for Y gradient
        private float CalculateSobelY(byte[,] grayscale, int x, int y, int width, int height)
        {
            // Sobel Y kernel:
            // -1 -2 -1
            //  0  0  0
            //  1  2  1

            float sum = 0.0f;

            // Top row
            sum += GetGrayscaleSafe(grayscale, x - 1, y - 1, width, height) * -1.0f;
            sum += GetGrayscaleSafe(grayscale, x, y - 1, width, height) * -2.0f;
            sum += GetGrayscaleSafe(grayscale, x + 1, y - 1, width, height) * -1.0f;

            // Bottom row
            sum += GetGrayscaleSafe(grayscale, x - 1, y + 1, width, height) * 1.0f;
            sum += GetGrayscaleSafe(grayscale, x, y + 1, width, height) * 2.0f;
            sum += GetGrayscaleSafe(grayscale, x + 1, y + 1, width, height) * 1.0f;

            return sum / 255.0f; // Normalize to [-1, 1] range
        }

        // Safe grayscale access with boundary clamping
        private byte GetGrayscaleSafe(byte[,] grayscale, int x, int y, int width, int height)
        {
            x = Math.Max(0, Math.Min(width - 1, x));
            y = Math.Max(0, Math.Min(height - 1, y));
            return grayscale[x, y];
        }

        // Gaussian blur implementation
        private byte[,] ApplyGaussianBlur(byte[,] input, int width, int height, int radius)
        {
            byte[,] output = new byte[width, height];
            float sigma = radius / 3.0f;
            float[,] kernel = CreateGaussianKernel(radius, sigma);
            int kernelSize = radius * 2 + 1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0.0f;
                    float weightSum = 0.0f;

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int px = Math.Max(0, Math.Min(width - 1, x + kx));
                            int py = Math.Max(0, Math.Min(height - 1, y + ky));

                            float weight = kernel[kx + radius, ky + radius];
                            sum += input[px, py] * weight;
                            weightSum += weight;
                        }
                    }

                    output[x, y] = (byte)(sum / weightSum);
                }
            }

            return output;
        }

        // Create Gaussian kernel
        private float[,] CreateGaussianKernel(int radius, float sigma)
        {
            int size = radius * 2 + 1;
            float[,] kernel = new float[size, size];
            float sum = 0.0f;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    float value = (float)(Math.Exp(-(x * x + y * y) / (2 * sigma * sigma)) / (2 * Math.PI * sigma * sigma));
                    kernel[x + radius, y + radius] = value;
                    sum += value;
                }
            }

            // Normalize
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    kernel[x, y] /= sum;
                }
            }

            return kernel;
        }
    }
}
