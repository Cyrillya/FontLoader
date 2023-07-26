using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.ModLoader.Config.UI;

namespace FontLoader.ConfigElements;

public class LoremIpsumElement : ConfigElement
{
    public override void OnBind() {
        base.OnBind();
        DrawLabel = false;
        
        Append(new UIText(Label, 0.6f, true) {
            Top = {Pixels = 8f},
            Width = {Percent = 1f},
            Height = {Pixels = 40f},
            TextOriginX = 0.5f,
            TextOriginY = 0.5f
        });
        
        Append(new UIText(Language.GetTextValue("Mods.FontLoader.Configs.Config.LoremIpsum.Subtitle"), 0.7f) {
            Top = {Pixels = 40f},
            Width = {Percent = 1f},
            Height = {Pixels = 40f},
            TextOriginX = 0.5f,
            TextOriginY = 0.5f
        });

        var uiText = new UIText(TooltipFunction(), 0.92f) {
            Top = {Pixels = 82f},
            Left = {Pixels = 10f},
            Width = {Pixels = -20f, Percent = 1f},
            IsWrapped = true,
        };
        uiText.OnInternalTextChange += () => {
            float newHeight = uiText.MinHeight.Pixels + 80f;
            Height.Set(newHeight, 0f);

            if (Parent is UISortableElement) {
                Parent.Height.Pixels = newHeight;
            }
        };
        Append(uiText);

        TooltipFunction = null;
    }
}