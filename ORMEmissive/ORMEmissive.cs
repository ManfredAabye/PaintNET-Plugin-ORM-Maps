using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace ORMEmissiveEffect
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author => "ManfredAabye";
        public string Copyright => "Copyright © 2025";
        public string DisplayName => "Emissive Map Generator";
        public Version Version => new Version(1, 0, 0, 0);
        public Uri WebsiteUri => new Uri("https://github.com/ManfredAabye");
    }

    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public class ORMEmissive : PropertyBasedEffect
    {
        // Property names
        private enum PropertyNames
        {
            Mode,
            Threshold,
            Intensity,
            UseOriginalColor,
            SaturationBoost,
            GlowRadius
        }

        // Emissive generation modes
        private enum EmissiveMode
        {
            Brightness,
            Saturation,
            BrightnessAndSaturation,
            ManualThreshold
        }

        // Constructor
        public ORMEmissive()
            : base("Emissive Map Generator", 
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
                new StaticListChoiceProperty(PropertyNames.Mode, new object[] 
                {
                    EmissiveMode.Brightness,
                    EmissiveMode.Saturation,
                    EmissiveMode.BrightnessAndSaturation,
                    EmissiveMode.ManualThreshold
                }, 0),
                new DoubleProperty(PropertyNames.Threshold, 0.7, 0.0, 1.0),
                new DoubleProperty(PropertyNames.Intensity, 1.0, 0.0, 10.0),
                new BooleanProperty(PropertyNames.UseOriginalColor, true),
                new DoubleProperty(PropertyNames.SaturationBoost, 1.0, 0.0, 3.0),
                new Int32Property(PropertyNames.GlowRadius, 0, 0, 10)
            };

            return new PropertyCollection(props);
        }

        // Control info setup
        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.Mode, ControlInfoPropertyNames.DisplayName, "Mode");
            PropertyControlInfo modeControl = configUI.FindControlForPropertyName(PropertyNames.Mode);
            modeControl.SetValueDisplayName(EmissiveMode.Brightness, "Brightness (Helligkeit)");
            modeControl.SetValueDisplayName(EmissiveMode.Saturation, "Saturation (Sättigung)");
            modeControl.SetValueDisplayName(EmissiveMode.BrightnessAndSaturation, "Brightness + Saturation");
            modeControl.SetValueDisplayName(EmissiveMode.ManualThreshold, "Manual Threshold");
            configUI.SetPropertyControlValue(PropertyNames.Mode, ControlInfoPropertyNames.Description, "Emissive-Erkennungsmodus");

            configUI.SetPropertyControlValue(PropertyNames.Threshold, ControlInfoPropertyNames.DisplayName, "Threshold");
            configUI.SetPropertyControlValue(PropertyNames.Threshold, ControlInfoPropertyNames.SliderLargeChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.Threshold, ControlInfoPropertyNames.SliderSmallChange, 0.01);
            configUI.SetPropertyControlValue(PropertyNames.Threshold, ControlInfoPropertyNames.UpDownIncrement, 0.01);
            configUI.SetPropertyControlValue(PropertyNames.Threshold, ControlInfoPropertyNames.DecimalPlaces, 2);
            configUI.SetPropertyControlValue(PropertyNames.Threshold, ControlInfoPropertyNames.Description, "Schwellenwert für Emission (0.0 - 1.0)");

            configUI.SetPropertyControlValue(PropertyNames.Intensity, ControlInfoPropertyNames.DisplayName, "Intensity");
            configUI.SetPropertyControlValue(PropertyNames.Intensity, ControlInfoPropertyNames.SliderLargeChange, 1.0);
            configUI.SetPropertyControlValue(PropertyNames.Intensity, ControlInfoPropertyNames.SliderSmallChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.Intensity, ControlInfoPropertyNames.UpDownIncrement, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.Intensity, ControlInfoPropertyNames.DecimalPlaces, 1);
            configUI.SetPropertyControlValue(PropertyNames.Intensity, ControlInfoPropertyNames.Description, "Emissive-Intensität (Multiplikator)");

            configUI.SetPropertyControlValue(PropertyNames.UseOriginalColor, ControlInfoPropertyNames.DisplayName, "Use Original Color");
            configUI.SetPropertyControlValue(PropertyNames.UseOriginalColor, ControlInfoPropertyNames.Description, "Original-Farbe beibehalten (sonst Weiß)");

            configUI.SetPropertyControlValue(PropertyNames.SaturationBoost, ControlInfoPropertyNames.DisplayName, "Saturation Boost");
            configUI.SetPropertyControlValue(PropertyNames.SaturationBoost, ControlInfoPropertyNames.SliderLargeChange, 0.5);
            configUI.SetPropertyControlValue(PropertyNames.SaturationBoost, ControlInfoPropertyNames.SliderSmallChange, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.SaturationBoost, ControlInfoPropertyNames.UpDownIncrement, 0.1);
            configUI.SetPropertyControlValue(PropertyNames.SaturationBoost, ControlInfoPropertyNames.DecimalPlaces, 1);
            configUI.SetPropertyControlValue(PropertyNames.SaturationBoost, ControlInfoPropertyNames.Description, "Sättigung verstärken");

            configUI.SetPropertyControlValue(PropertyNames.GlowRadius, ControlInfoPropertyNames.DisplayName, "Glow Radius");
            configUI.SetPropertyControlValue(PropertyNames.GlowRadius, ControlInfoPropertyNames.Description, "Glow-Effekt (0 = aus)");

            return configUI;
        }

        // Properties
        private EmissiveMode mode;
        private double threshold;
        private double intensity;
        private bool useOriginalColor;
        private double saturationBoost;
        private int glowRadius;

        // Update properties from UI
        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            this.mode = (EmissiveMode)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.Mode).Value;
            this.threshold = newToken.GetProperty<DoubleProperty>(PropertyNames.Threshold).Value;
            this.intensity = newToken.GetProperty<DoubleProperty>(PropertyNames.Intensity).Value;
            this.useOriginalColor = newToken.GetProperty<BooleanProperty>(PropertyNames.UseOriginalColor).Value;
            this.saturationBoost = newToken.GetProperty<DoubleProperty>(PropertyNames.SaturationBoost).Value;
            this.glowRadius = newToken.GetProperty<Int32Property>(PropertyNames.GlowRadius).Value;

            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        // Main rendering function
        protected override void OnRender(Rectangle[] rois, int startIndex, int length)
        {
            if (length == 0) return;

            Surface dst = DstArgs.Surface;
            Surface src = SrcArgs.Surface;

            // Create emissive buffer
            ColorBgra[,] emissiveBuffer = new ColorBgra[src.Width, src.Height];

            // First pass: Calculate emissive values
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    ColorBgra pixel = src[x, y];
                    float emissiveValue = CalculateEmissiveValue(pixel);

                    if (useOriginalColor)
                    {
                        // Use original color with intensity
                        byte r = ClampByte(pixel.R * emissiveValue * (float)intensity);
                        byte g = ClampByte(pixel.G * emissiveValue * (float)intensity);
                        byte b = ClampByte(pixel.B * emissiveValue * (float)intensity);
                        
                        // Apply saturation boost if needed
                        if (saturationBoost != 1.0)
                        {
                            ColorBgra boosted = BoostSaturation(ColorBgra.FromBgra(b, g, r, 255), saturationBoost);
                            emissiveBuffer[x, y] = boosted;
                        }
                        else
                        {
                            emissiveBuffer[x, y] = ColorBgra.FromBgra(b, g, r, 255);
                        }
                    }
                    else
                    {
                        // White emission with intensity
                        byte value = ClampByte(255 * emissiveValue * (float)intensity);
                        emissiveBuffer[x, y] = ColorBgra.FromBgra(value, value, value, 255);
                    }
                }
            }

            // Apply glow if needed
            if (glowRadius > 0)
            {
                emissiveBuffer = ApplyGlow(emissiveBuffer, src.Width, src.Height, glowRadius);
            }

            // Second pass: Write to destination
            for (int i = startIndex; i < startIndex + length; i++)
            {
                Rectangle rect = rois[i];

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        dst[x, y] = emissiveBuffer[x, y];
                    }
                }
            }
        }

        // Calculate emissive value based on mode
        private float CalculateEmissiveValue(ColorBgra pixel)
        {
            float brightness = (pixel.R + pixel.G + pixel.B) / (3.0f * 255.0f);
            float saturation = CalculateSaturation(pixel);

            switch (mode)
            {
                case EmissiveMode.Brightness:
                    return brightness >= (float)threshold ? 1.0f : 0.0f;

                case EmissiveMode.Saturation:
                    return saturation >= (float)threshold ? 1.0f : 0.0f;

                case EmissiveMode.BrightnessAndSaturation:
                    return (brightness >= (float)threshold && saturation >= (float)threshold) ? 1.0f : 0.0f;

                case EmissiveMode.ManualThreshold:
                    // Smooth transition around threshold
                    float combined = (brightness + saturation) / 2.0f;
                    if (combined >= (float)threshold)
                    {
                        return Math.Min(1.0f, (combined - (float)threshold) / (1.0f - (float)threshold));
                    }
                    return 0.0f;

                default:
                    return 0.0f;
            }
        }

        // Calculate saturation
        private float CalculateSaturation(ColorBgra pixel)
        {
            int max = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
            int min = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));

            if (max == 0)
                return 0.0f;

            return (float)(max - min) / (float)max;
        }

        // Boost saturation
        private ColorBgra BoostSaturation(ColorBgra pixel, double boost)
        {
            // Convert to HSV
            int max = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
            int min = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
            float delta = max - min;

            if (delta == 0)
                return pixel; // Gray pixel, no saturation

            float saturation = delta / (float)max;
            float value = max / 255.0f;

            // Boost saturation
            saturation = Math.Min(1.0f, saturation * (float)boost);

            // Convert back to RGB
            float h = 0.0f;
            if (pixel.R == max)
                h = (pixel.G - pixel.B) / delta;
            else if (pixel.G == max)
                h = 2.0f + (pixel.B - pixel.R) / delta;
            else
                h = 4.0f + (pixel.R - pixel.G) / delta;

            h *= 60.0f;
            if (h < 0.0f) h += 360.0f;

            // HSV to RGB
            float c = value * saturation;
            float x = c * (1.0f - Math.Abs((h / 60.0f) % 2.0f - 1.0f));
            float m = value - c;

            float r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }

            return ColorBgra.FromBgra(
                (byte)((b + m) * 255.0f),
                (byte)((g + m) * 255.0f),
                (byte)((r + m) * 255.0f),
                255);
        }

        // Apply glow effect
        private ColorBgra[,] ApplyGlow(ColorBgra[,] input, int width, int height, int radius)
        {
            ColorBgra[,] output = new ColorBgra[width, height];
            float sigma = radius / 3.0f;
            float[,] kernel = CreateGaussianKernel(radius, sigma);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float r = 0, g = 0, b = 0;
                    float weightSum = 0.0f;

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int px = Math.Max(0, Math.Min(width - 1, x + kx));
                            int py = Math.Max(0, Math.Min(height - 1, y + ky));

                            float weight = kernel[kx + radius, ky + radius];
                            ColorBgra pixel = input[px, py];

                            r += pixel.R * weight;
                            g += pixel.G * weight;
                            b += pixel.B * weight;
                            weightSum += weight;
                        }
                    }

                    output[x, y] = ColorBgra.FromBgra(
                        (byte)(b / weightSum),
                        (byte)(g / weightSum),
                        (byte)(r / weightSum),
                        255);
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

        // Clamp byte value
        private byte ClampByte(double value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return (byte)value;
        }
    }
}
