﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FontLoader.Core;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Velentr.Font;

namespace FontLoader.ConfigElements;

public class Config : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [CustomModConfigItem(typeof(OpenConfigElement))]
    public object OpenConfigOption;

    [CustomModConfigItem(typeof(FontSelectionElement))]
    [DefaultValue("NONE")]
    public string FontPath;

    [CustomModConfigItem(typeof(FontSelectionElement))]
    [DefaultValue("NONE")]
    public string AltFontPath;

    [CustomModConfigItem(typeof(FontSearchDirectoryElement))]
    [JsonDefaultValue("{}")]
    public FontSearchDirectory FontSearchPath;
    
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

    [CustomModConfigItem(typeof(ResetElement))]
    public object ResetOption;

    public override void OnChanged() {
        base.OnChanged();
        
        if (!Loader.ModLoaded)
            return;
        
        if (Loader.InstalledFontLoading)
            return;

        if (Statics.Manager is not null) {
            Unloader.UnloadFonts();
        }

        Loader.ProvideFonts(FontPath, AltFontPath);
        Task.Run(Loader.LoadInstalledFonts);
    }
}