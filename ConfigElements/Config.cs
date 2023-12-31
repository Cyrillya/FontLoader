﻿using System.ComponentModel;
using System.Threading.Tasks;
using FontLoader.Core;
using Terraria.ModLoader.Config;

namespace FontLoader.ConfigElements;

public class Config : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [CustomModConfigItem(typeof(OpenConfigElement))]
    public object OpenConfigOption;

    [ReloadRequired]
    [DefaultValue("NONE")]
    [CustomModConfigItem(typeof(FontSelectionElement))]
    public string FontPath;

    [ReloadRequired]
    [DefaultValue("NONE")]
    [CustomModConfigItem(typeof(FontSelectionElement))]
    public string AltFontPath;

    [ReloadRequired]
    [JsonDefaultValue("{}")]
    [CustomModConfigItem(typeof(FontSearchDirectoryElement))]
    public FontSearchDirectory FontSearchPath;
    
    [Slider]
    [ReloadRequired]
    [DefaultValue(1f)]
    [Range(0.75f, 1.25f)]
    [Increment(0.05f)]
    public float FontScale;

    [Slider]
    [DefaultValue(0f)]
    [Range(-10f, 10f)]
    [Increment(1f)]
    [CustomModConfigItem(typeof(FontOffsetPreview))]
    public float GeneralFontOffsetY;

    [Slider]
    [DefaultValue(0f)]
    [Range(-20f, 20f)]
    [Increment(1f)]
    [CustomModConfigItem(typeof(BigFontOffsetPreview))]
    public float BigFontOffsetY;

    [DefaultValue(true)]
    public bool UseTextShadow;

    [ReloadRequired]
    [DefaultValue(false)]
    public bool UsePingFangLite;

    [ReloadRequired]
    [DefaultValue(true)]
    public bool SetupFontPreview;

    [ReloadRequired]
    [DefaultValue(false)]
    public bool DebugText;

    [CustomModConfigItem(typeof(LoremIpsumElement))]
    public object LoremIpsum;
}