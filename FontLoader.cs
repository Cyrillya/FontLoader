using FontLoader.Core;
using Terraria.ModLoader;
using Loader = FontLoader.Core.Loader;

namespace FontLoader;

public class FontLoader : Mod
{
    public static FontLoader Instance { get; private set; }
    public override void Load() {
        Instance = this;
        Loader.Load(this);
    }

    public override void Unload() {
        Unloader.Unload();
        Instance = null;
    }
}