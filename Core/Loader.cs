using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FontLoader.ConfigElements;
using FontLoader.Utilities;
using Microsoft.Xna.Framework.Graphics;
using Velentr.Font;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FontLoader.Core;

public static class Loader
{
    public static bool ModLoaded { get; private set; }
    public static bool InstalledFontLoading { get; private set; }

    public static void Load(Mod mod) {
        if (!OperatingSystem.IsWindows()) {
            throw new PlatformNotSupportedException(
                Language.GetTextValue(mod.GetLocalizationKey("PlatformNotSupported")));
        }

        var config = ModContent.GetInstance<Config>();

        ProvideFreeTypeDll(mod);
        LoadInternalFonts(mod);
        ProvideFonts(config.FontPath, config.AltFontPath);
        LoadInstalledFonts();
        DetourLoader.Load();
        RenderTargetHolder.Load();
        // TestContents.Load();

        ModLoaded = true;
    }

    internal static void ProvideFonts(string mainPath, string altPath) {
        mainPath ??= "";
        altPath ??= "";
        byte[] mainFontBytes;
        byte[] altFontBytes;
        if (!ModUtilities.IsTtfOrOtfFile(mainPath)) {
            if (!ModUtilities.IsTtfOrOtfFile(altPath)) {
                mainFontBytes = FontStatics.PingFangBytes;
                altFontBytes = FontStatics.RobotoBytes;
                mainPath = "PingFangInternal";
                altPath = "RobotoInternal";
            }
            else {
                mainFontBytes = File.ReadAllBytes(altPath);
                altFontBytes = mainFontBytes;
                mainPath = altPath;
            }
        }
        else {
            mainFontBytes = File.ReadAllBytes(mainPath);
            if (!ModUtilities.IsTtfOrOtfFile(altPath)) {
                altFontBytes = mainFontBytes;
                altPath = mainPath;
            }
            else {
                altFontBytes = File.ReadAllBytes(altPath);
            }
        }

        FontCollection GetFontCollection(int size) =>
            new(FontStatics.Manager, mainPath, mainFontBytes, altPath, altFontBytes, size);

        // var fontPath = @"C:\Users\Administrator\AppData\Local\Microsoft\FontCache\4\CloudFonts\Microsoft YaHei\47849284094.ttf";
        FontStatics.Manager = new FontManager(Main.instance.GraphicsDevice);
        FontStatics.FontMouseText = GetFontCollection(18);
        FontStatics.FontCombatText = GetFontCollection(19);
        FontStatics.FontCombatCrit = GetFontCollection(24);
        FontStatics.FontItemStack = GetFontCollection(15);
        FontStatics.FontDeathText = GetFontCollection(45);
    }

    internal static void LoadInstalledFonts() {
        // 在Config里写了开启新线程加载，防止多个线程同时执行方法，这里加个判断
        if (InstalledFontLoading) return;

        var config = ModContent.GetInstance<Config>();

        InstalledFontLoading = true;

        var fontsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        if (config.FontSearchPath?.Mode is SearchPathMode.CustomPath) {
            string customPath = config.FontSearchPath?.CustomPath;
            if (Directory.Exists(customPath)) {
                fontsFolderPath = customPath;
            }
            else {
                return;
            }
        }
        // fontsFolderPath = @"F:\Downloads\Compressed\苹方字体19.0d3e2版本_2";

        var fontFiles = Directory.GetFiles(fontsFolderPath, "*.*", SearchOption.AllDirectories)
            .Where(ModUtilities.IsTtfOrOtfFile);

        var allFontFiles = fontFiles;

        if (OperatingSystem.IsWindows() && config.FontSearchPath?.Mode is SearchPathMode.SystemAndUser) {
            var localFontsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            localFontsFolderPath = Path.Combine(localFontsFolderPath, @"Microsoft\Windows\Fonts");
            var localFontFiles = Directory.GetFiles(localFontsFolderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(ModUtilities.IsTtfOrOtfFile);
            allFontFiles = allFontFiles.Concat(localFontFiles);
        }

        foreach (var fontFile in allFontFiles) {
            FontStatics.Manager.GetMinimalFont(fontFile);
        }

        InstalledFontLoading = false;
    }

    internal static void LoadInternalFonts(Mod mod) {
        FontStatics.PingFangBytes = mod.GetFileBytes("Assets/PingFang.otf");
        FontStatics.RobotoBytes = mod.GetFileBytes("Assets/Roboto.ttf");
    }

    private static void ProvideFreeTypeDll(Mod mod) {
        string targetFilePath = AppDomain.CurrentDomain.BaseDirectory; // 保存目标路径
        const string targetFileName = @"Libraries\Native\Windows\freetype6.dll"; // 保存目标文件名

        string fullPath = Path.Combine(targetFilePath, targetFileName);

        if (!ModUtilities.IsFileOccupied(fullPath)) {
            try {
                using var fileStream = mod.GetFileStream("freetype6.dll");
                using var targetStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);

                fileStream.CopyTo(targetStream);

                Console.WriteLine(Language.GetTextValue(mod.GetLocalizationKey("FreeTypeSaved")) +
                                  targetFilePath);
            }
            catch (Exception ex) {
                mod.Logger.Warn(
                    Language.GetTextValue(mod.GetLocalizationKey("FreeTypeSaveError")), ex);
            }

            NativeLibrary.Load(fullPath);
        }
    }
}