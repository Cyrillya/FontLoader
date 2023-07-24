using System.Diagnostics;
using System.IO;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace FontLoader.ConfigElements;

internal class OpenConfigElement : OpenFileElement
{
    protected override string FilePath => Path.Combine(ConfigManager.ModConfigPath, "FontLoader_Config.json");
}

internal abstract class OpenFileElement : LargerPanelElement
{
    protected abstract string FilePath { get; }

    public override void LeftClick(UIMouseEvent evt) {
        base.LeftClick(evt);

        if (!File.Exists(FilePath)) return;
        Process.Start(new ProcessStartInfo(FilePath)
        {
            UseShellExecute = true
        });
    }
}