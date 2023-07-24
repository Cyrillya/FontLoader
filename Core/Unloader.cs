using Velentr.Font;

namespace FontLoader.Core;

public static class Unloader
{
    public static bool Unloading { get; private set; }

    public static void Unload() {
        Unloading = true;

        UnloadFonts();
        RenderTargetHolder.Unload();
        UnloadStatics();
        // TestContents.Unload();

        Unloading = false;
    }
    
    internal static void UnloadStatics() {
        Statics.PingFangBytes = null;
        Statics.LoadModsField = null;
        Statics.SetTextMethod = null;
    }

    internal static void UnloadFonts() {
        Statics.Manager?.Dispose();
        Statics.FontDeathText = null;
        Statics.FontMouseText = null;
        Statics.FontCombatText = null;
        Statics.FontCombatCrit = null;
        Statics.FontItemStack = null;
        Statics.Manager = null;
    }
}