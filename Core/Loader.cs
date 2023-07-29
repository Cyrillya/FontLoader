using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using FontLoader.ConfigElements;
using FontLoader.Utilities;
using Velentr.Font;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FontLoader.Core;

public static class Loader
{
    public static void Load(Mod mod) {
        if (!OperatingSystem.IsWindows()) {
            throw new PlatformNotSupportedException(
                Language.GetTextValue(mod.GetLocalizationKey("PlatformNotSupported")));
        }

        ProvideFreeTypeDll(mod);
        LoadInternalFont(mod);
        ProvideFonts();
        DetourLoader.Load();
        FontPreviewHolder.Load();
        // TestContents.Load();
    }

    internal static void ProvideFonts() {
        ModUtilities.SetLoadingText(LocalizationKey.ApplyingFonts);

        var config = ModContent.GetInstance<Config>();
        string mainPath = config.FontPath ?? "";
        string altPath = config.AltFontPath ?? "";
        byte[] mainFontBytes;
        byte[] altFontBytes;
        if (!FontFileTypeChecker.IsFontFile(mainPath)) {
            if (!FontFileTypeChecker.IsFontFile(altPath)) {
                mainFontBytes = Statics.PingFangBytes;
                altFontBytes = Statics.PingFangBytes;
                mainPath = "PingFangInternal";
                altPath = "PingFangInternal";
            }
            else {
                mainFontBytes = File.ReadAllBytes(altPath);
                altFontBytes = mainFontBytes;
                mainPath = altPath;
            }
        }
        else {
            mainFontBytes = File.ReadAllBytes(mainPath);
            if (!FontFileTypeChecker.IsFontFile(altPath)) {
                altFontBytes = mainFontBytes;
                altPath = mainPath;
            }
            else {
                altFontBytes = File.ReadAllBytes(altPath);
            }
        }

        int GetSize(int baseSize) => (int) (baseSize * config.FontScale);

        FontCollection GetFontCollection(int baseSize) =>
            new(Statics.Manager, mainPath, mainFontBytes, altPath, altFontBytes, GetSize(baseSize));

        // var fontPath = @"C:\Users\Administrator\AppData\Local\Microsoft\FontCache\4\CloudFonts\Microsoft YaHei\47849284094.ttf";
        Statics.Manager = new FontManager(Main.instance.GraphicsDevice);
        Statics.FontMouseText = GetFontCollection(18);
        Statics.FontCombatText = GetFontCollection(19);
        Statics.FontCombatCrit = GetFontCollection(24);
        Statics.FontItemStack = GetFontCollection(15);
        Statics.FontDeathText = GetFontCollection(45);
    }

    private static void LoadInternalFont(Mod mod) {
        var config = ModContent.GetInstance<Config>();
        if (config.UsePingFangLite) {
            ModUtilities.SetLoadingText(LocalizationKey.LoadingInternal);
            Statics.PingFangBytes = mod.GetFileBytes("Assets/PingFangLite.otf");
            return;
        }

        ModUtilities.SetLoadingText(LocalizationKey.DecompressingInternal);

        var compressedBytes = mod.GetFileBytes("Assets/PingFang.otf.lzma");
        var inStream = new MemoryStream(compressedBytes);
        var outStream = new MemoryStream();
        ModUtilities.Decompress(inStream, outStream);
        outStream.Position = 0;
        Statics.PingFangBytes = new byte[outStream.Length];
        outStream.Read(Statics.PingFangBytes, 0, Statics.PingFangBytes.Length);
    }

    private static void ProvideFreeTypeDll(Mod mod) {
        ModUtilities.SetLoadingText(LocalizationKey.DecompressingDLL);

        string targetFilePath = AppDomain.CurrentDomain.BaseDirectory; // 保存目标路径
        const string targetFileName = @"Libraries\Native\Windows\freetype6.dll"; // 保存目标文件名

        string fullPath = Path.Combine(targetFilePath, targetFileName);

        // 如果Native文件夹下已经有freetype6.dll，tModLoader会自动加载，不需要我们自己加载
        if (File.Exists(fullPath)) {
            return;
        }

        try {
            using var fileStream = mod.GetFileStream("Assets/freetype6.dll");
            using var targetStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);

            fileStream.CopyTo(targetStream);

            Console.WriteLine(Language.GetTextValue(mod.GetLocalizationKey("FreeTypeSaved")) +
                              targetFilePath);

            NativeLibrary.TryLoad(fullPath, out _);
        }
        catch (Exception ex) {
            mod.Logger.Warn(Language.GetTextValue(mod.GetLocalizationKey("FreeTypeSaveError")), ex);
        }
    }
}