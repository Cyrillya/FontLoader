using FontLoader.Core;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace FontLoader.ConfigElements;

public class ResetElement : LargerPanelElement
{
    protected override UIText ProvideUIText() {
        var uiText = base.ProvideUIText();
        uiText.TextColor = Color.Red;
        return uiText;
    }

    public override void LeftClick(UIMouseEvent evt) {
        base.LeftClick(evt);

        Unloader.UnloadFonts();
        Loader.ProvideFonts();
    }
}