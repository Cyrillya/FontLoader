namespace FontLoader.Core;

public static class Unloader
{
    public static bool Unloading { get; private set; }

    public static void Unload() {
        Unloading = true;

        UnloadFonts();
        FontPreviewHolder.Unload();
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
        Statics.FontDeathText?.Dispose();
        Statics.FontMouseText?.Dispose();
        Statics.FontCombatText?.Dispose();
        Statics.FontCombatCrit?.Dispose();
        Statics.FontItemStack?.Dispose();
        Statics.FontDeathText = null;
        Statics.FontMouseText = null;
        Statics.FontCombatText = null;
        Statics.FontCombatCrit = null;
        Statics.FontItemStack = null;
        Statics.Manager = null;
    }
}