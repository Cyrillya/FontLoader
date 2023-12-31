﻿using System.Collections.Generic;
using Terraria.Localization;

namespace FontLoader.Core;

public enum LocalizationKey
{
    DecompressingDLL,
    ApplyingFonts,
    LoadingInternal,
    DecompressingInternal,
    AddingDetours,
    LoadingInstalled,
    SettingPreview
}

public class PreLoadLocalization
{
    private readonly Dictionary<LocalizationKey, string> _englishLookup = new() {
        {LocalizationKey.DecompressingDLL, "Decompressing freetype6.dll"},
        {LocalizationKey.ApplyingFonts, "Applying Selected Fonts"},
        {LocalizationKey.LoadingInternal, "Loading Internal Font"},
        {LocalizationKey.DecompressingInternal, "Decompressing Internal Font"},
        {LocalizationKey.AddingDetours, "Adding Detours"},
        {LocalizationKey.LoadingInstalled, "Loading Installed Fonts"},
        {LocalizationKey.SettingPreview, "Constructing Font Previews"}
    };

    private readonly Dictionary<LocalizationKey, string> _chineseLookup = new() {
        {LocalizationKey.DecompressingDLL, "正在解压 freetype6.dll"},
        {LocalizationKey.ApplyingFonts, "正在应用选定字体"},
        {LocalizationKey.LoadingInternal, "正在加载内置字体"},
        {LocalizationKey.DecompressingInternal, "正在解压内置字体"},
        {LocalizationKey.AddingDetours, "正在添加Detour"},
        {LocalizationKey.LoadingInstalled, "正在加载已安装字体"},
        {LocalizationKey.SettingPreview, "正在构造字体预览"}
    };

    public static string GetLocalizedText(LocalizationKey key) {
        var instance = new PreLoadLocalization();
        return Language.ActiveCulture.Name switch {
            "zh-Hans" => instance._chineseLookup[key],
            _ => instance._englishLookup[key]
        };
    }
}