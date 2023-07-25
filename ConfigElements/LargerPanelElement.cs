using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace FontLoader.ConfigElements;

public class LargerPanelElement : ConfigElement
{
    public override void OnBind() {
        base.OnBind();
        Height.Set(36f, 0f);
        DrawLabel = false;

        Append(ProvideUIText());
    }
    
    protected virtual UIText ProvideUIText() => new(Label, 0.4f, true) {
        Top = {Pixels = 4f},
        TextOriginX = 0.5f,
        TextOriginY = 0.5f,
        Width = StyleDimension.Fill,
        Height = StyleDimension.Fill
    };
}