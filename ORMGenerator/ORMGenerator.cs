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
        public string Author => "Manfred Aabye";
        public string Copyright => "Copyright © 2025";
        public string DisplayName => "ORM Map Generator";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public Uri WebsiteUri => new Uri("https://github.com/manfredaabye/ormgenerator");
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
        private MaterialPreset materialPreset;

        public enum MapSource
        {
            RedChannel,
            GreenChannel,
            BlueChannel,
            AlphaChannel,
            Grayscale,
            CustomValue
        }

        public enum MaterialPreset
        {
            Custom,
            PolishedMetal,
            RoughMetal,
            BrushedMetal,
            Wood,
            RoughWood,
            Stone,
            Concrete,
            Plastic,
            Rubber,
            Glass,
            Fabric,
            Leather,
            Ceramic,
            Paint
        }

        public ORMGenerator()
            : base(
                "ORM Map Generator", 
                (System.Drawing.Image?)null, 
                SubmenuNames.Render, 
                new EffectOptions() 
                { 
                    Flags = EffectFlags.Configurable,
                    RenderingSchedule = EffectRenderingSchedule.None
                })
        {
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            // Material Presets
            props.Add(new StaticListChoiceProperty("MaterialPreset", 
                new object[] 
                {
                    MaterialPreset.Custom,
                    MaterialPreset.PolishedMetal,
                    MaterialPreset.RoughMetal,
                    MaterialPreset.BrushedMetal,
                    MaterialPreset.Wood,
                    MaterialPreset.RoughWood,
                    MaterialPreset.Stone,
                    MaterialPreset.Concrete,
                    MaterialPreset.Plastic,
                    MaterialPreset.Rubber,
                    MaterialPreset.Glass,
                    MaterialPreset.Fabric,
                    MaterialPreset.Leather,
                    MaterialPreset.Ceramic,
                    MaterialPreset.Paint
                }, 
                0));

            // Auto Detection
            props.Add(new BooleanProperty("AutoDetection", false));
            
            // Roughness Properties
            props.Add(new StaticListChoiceProperty("RoughnessSource", 
                new object[]
                {
                    MapSource.RedChannel,
                    MapSource.GreenChannel,
                    MapSource.BlueChannel,
                    MapSource.AlphaChannel,
                    MapSource.Grayscale,
                    MapSource.CustomValue
                }, 
                4)); // Default: Grayscale
            props.Add(new DoubleProperty("Roughness", 0.5, 0.0, 1.0));
            props.Add(new BooleanProperty("InvertRoughness", false));
            
            // Metallic Properties
            props.Add(new StaticListChoiceProperty("MetallicSource", 
                new object[]
                {
                    MapSource.RedChannel,
                    MapSource.GreenChannel,
                    MapSource.BlueChannel,
                    MapSource.AlphaChannel,
                    MapSource.Grayscale,
                    MapSource.CustomValue
                }, 
                5)); // Default: CustomValue
            props.Add(new DoubleProperty("Metallic", 0.0, 0.0, 1.0));
            props.Add(new BooleanProperty("InvertMetallic", false));
            
            // Ambient Occlusion Properties
            props.Add(new StaticListChoiceProperty("AOSource", 
                new object[]
                {
                    MapSource.RedChannel,
                    MapSource.GreenChannel,
                    MapSource.BlueChannel,
                    MapSource.AlphaChannel,
                    MapSource.Grayscale,
                    MapSource.CustomValue
                }, 
                0)); // Default: RedChannel
            props.Add(new DoubleProperty("AmbientOcclusion", 1.0, 0.0, 1.0));
            props.Add(new BooleanProperty("InvertAO", false));
            
            // Preview
            props.Add(new BooleanProperty("PreviewMode", false));

            return new PropertyCollection(props);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            // Material Presets
            configUI.SetPropertyControlValue("MaterialPreset", ControlInfoPropertyNames.DisplayName, "Material Preset");
            configUI.SetPropertyControlValue("MaterialPreset", ControlInfoPropertyNames.Description,
                "Wähle ein vordefiniertes Material (Custom für manuelle Einstellungen)");
            configUI.SetPropertyControlType("MaterialPreset", PropertyControlType.DropDown);

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

            return configUI;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, 
                                                RenderArgs dstArgs, 
                                                RenderArgs srcArgs)
        {
            // Material Preset zuerst laden
            this.materialPreset = (MaterialPreset)newToken.GetProperty<StaticListChoiceProperty>("MaterialPreset").Value;
            
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
            
            // Preset-Werte anwenden, falls nicht Custom
            if (this.materialPreset != MaterialPreset.Custom)
            {
                ApplyMaterialPreset();
            }
            
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
                        float intensity = (float)pixel.GetIntensity();
                        
                        // Manuelle Sättigungsberechnung
                        float max = Math.Max(Math.Max((float)pixel.R, (float)pixel.G), (float)pixel.B) / 255f;
                        float min = Math.Min(Math.Min((float)pixel.R, (float)pixel.G), (float)pixel.B) / 255f;
                        float saturation = max > 0 ? (max - min) / max : 0f;
                        
                        // Roughness: Dunkle Bereiche = rau, Helle = glatt
                        byte rValue = (byte)((1.0f - intensity) * 255f);
                        
                        // Metallic: Niedrige Sättigung = potenziell metallisch
                        byte mValue = 0;
                        if (saturation < 0.3f)
                        {
                            // Grautöne sind potenziell metallisch
                            mValue = (byte)((1.0f - (saturation * 3.0f)) * 255f);
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
                            // Export Modus - ORM Format: R=Occlusion, G=Roughness, B=Metallic
                            outputPixel = ColorBgra.FromBgra(
                                mValue,     // B = Metallic
                                rValue,     // G = Roughness
                                aoValue,    // R = Occlusion (AO)
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

        private void ApplyMaterialPreset()
        {
            switch (this.materialPreset)
            {
                case MaterialPreset.PolishedMetal:
                    this.roughness = 0.1;
                    this.metallic = 1.0;
                    this.ambientOcclusion = 1.0;
                    break;
                case MaterialPreset.RoughMetal:
                    this.roughness = 0.8;
                    this.metallic = 1.0;
                    this.ambientOcclusion = 0.9;
                    break;
                case MaterialPreset.BrushedMetal:
                    this.roughness = 0.4;
                    this.metallic = 1.0;
                    this.ambientOcclusion = 0.95;
                    break;
                case MaterialPreset.Wood:
                    this.roughness = 0.7;
                    this.metallic = 0.0;
                    this.ambientOcclusion = 0.85;
                    break;
                case MaterialPreset.RoughWood:
                    this.roughness = 0.9;
                    this.metallic = 0.0;
                    this.ambientOcclusion = 0.8;
                    break;
                case MaterialPreset.Stone:
                    this.roughness = 0.85;
                    this.metallic = 0.0;
                    this.ambientOcclusion = 0.7;
                    break;
                case MaterialPreset.Concrete:
                    this.roughness = 0.9;
                    this.metallic = 0.0;
                    this.ambientOcclusion = 0.75;
                    break;
                case MaterialPreset.Plastic:
                    this.roughness = 0.4;
                    this.metallic = 0.0;
                    this.ambientOcclusion = 1.0;
                    break;
                case MaterialPreset.Rubber:
                    this.roughness = 0.8;
                    this.metallic = 0.0;
                    this.ambientOcclusion = 0.9;
                    break;
                case MaterialPreset.Glass:
                    this.roughness = 0.05;
                    this.metallic = 0.0;
                    this.ambientOcclusion = 1.0;
                    break;
                case MaterialPreset.Fabric:
                    this.roughness = 0.85;
                    this.metallic = 0.0;
                    this.ambientOcclusion = 0.85;
                    break;
                case MaterialPreset.Leather:
                    this.roughness = 0.6;
                    this.metallic = 0.0;
                    this.ambientOcclusion = 0.9;
                    break;
                case MaterialPreset.Ceramic:
                    this.roughness = 0.3;
                    this.metallic = 0.0;
                    this.ambientOcclusion = 0.95;
                    break;
                case MaterialPreset.Paint:
                    this.roughness = 0.5;
                    this.metallic = 0.0;
                    this.ambientOcclusion = 1.0;
                    break;
            }
        }
    }
}
