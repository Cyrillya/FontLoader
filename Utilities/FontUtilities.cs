using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.GameContent;
using Velentr.Font;

namespace FontLoader.Utilities;

public static class FontUtilities
{

    public static FontCollection GetVelentrFont(this Asset<DynamicSpriteFont> dynamicSpriteFont) =>
        GetVelentrFont(dynamicSpriteFont.Value);
    
    public static FontCollection GetVelentrFont(this DynamicSpriteFont dynamicSpriteFont) {
        if (dynamicSpriteFont == FontAssets.MouseText.Value) {
            return FontLoader.FontMouseText;
        }
        if (dynamicSpriteFont == FontAssets.DeathText.Value) {
            return FontLoader.FontDeathText;
        }
        if (dynamicSpriteFont == FontAssets.ItemStack.Value) {
            return FontLoader.FontItemStack;
        }
        if (dynamicSpriteFont == FontAssets.CombatText[0].Value) {
            return FontLoader.FontCombatText;
        }
        if (dynamicSpriteFont == FontAssets.CombatText[1].Value) {
            return FontLoader.FontCombatCrit;
        }

        return null;
    }

    public static float GetSpreadMultipiler(this DynamicSpriteFont dynamicSpriteFont) {
        if (dynamicSpriteFont == FontAssets.DeathText.Value) {
            return 1.5f;
        }
        if (dynamicSpriteFont == FontAssets.CombatText[1].Value) {
            return 1.1f;
        }

        return 1f;
    }
}
