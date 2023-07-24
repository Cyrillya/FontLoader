using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;

namespace FontLoader.Core;

public static class TestContents
{
    public static void Load() {
        Main.OnPostDraw += DrawingTest;
    }

    public static void Unload() {
        Main.OnPostDraw -= DrawingTest;
    }

    private static void DrawingTest(GameTime gameTime) {
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

        var damnText = "Hello World! \n中国智造，慧及全球";
        FontStatics.FontDeathText.Draw(Main.spriteBatch, damnText, new Vector2(50, 50), Color.White);
        var wtf =
            @"You are protected, in short, by your ability to love! The only protection that can possibly work against the lure of power like Voldemort's! In spite of all the temptation you have endured, all the suffering, you remain pure of heart, just as pure as you were at the age of eleven, when you stared into a mirror that reflected your heart's desire, and it showed you only the way to thwart Lord Voldemort, and not immortality or riches. Harry, have you any idea how few wizards could have seen what you saw in that mirror?";
        var wrappedText = FontAssets.MouseText.Value.CreateWrappedText(wtf, 900);
        Main.spriteBatch.DrawString(FontAssets.MouseText.Value, wrappedText, new Vector2(100, 500), Color.White);

        Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.DeathText.Value, damnText, 100, 200, Color.White,
            Color.Black, Vector2.Zero, 1f);

        Main.spriteBatch.End();
    }
}