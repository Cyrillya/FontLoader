using Velentr.Font;

namespace FontLoader.Core;

public static class Unloader
{
    public static bool Unloading { get; private set; }

    public static void Unload() {
        Unloading = true;

        UnloadFonts();
        RenderTargetHolder.Unload();
        // TestContents.Unload();

        Unloading = false;
    }

    internal static void UnloadFonts() {
        FontStatics.Manager?.Dispose();
        FontStatics.FontDeathText = null;
        FontStatics.FontMouseText = null;
        FontStatics.FontCombatText = null;
        FontStatics.FontCombatCrit = null;
        FontStatics.FontItemStack = null;
        FontStatics.Manager = null;
    }
}