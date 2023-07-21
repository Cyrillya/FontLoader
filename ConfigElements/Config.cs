using Terraria.ModLoader.Config;

namespace FontLoader.ConfigElements;

public class Config : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [CustomModConfigItem(typeof(FontSelectionElement))]
    public string MouseTextFont;
}