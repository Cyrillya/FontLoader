using System;
using FontLoader.Core;
using Velentr.Font;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace FontLoader.ConfigElements;

public class FontElement : UIElement
{
    private readonly RenderTarget2D _renderTarget;
    public string Name { get; }

    public FontElement(RenderTarget2D target, string name) {
        Width.Set(0f, 1f);
        Height.Set(32f, 0f);
        _renderTarget = target;
        Name = name;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        var dimensions = GetDimensions();
        int settingsWidth = (int)Math.Ceiling(dimensions.Width - 4f);
        int height = (int)Math.Ceiling(dimensions.Height);
        var backgroundColor = UICommon.DefaultUIBlue;
        var panelColor = IsMouseHovering ? backgroundColor : backgroundColor.MultiplyRGBA(new Color(180, 180, 180));
        var position = dimensions.Position();
        ConfigElement.DrawPanel2(spriteBatch, position, TextureAssets.SettingsPanel.Value, settingsWidth, height,
            panelColor);

        position.X += 10;
        position.Y += 6;
        spriteBatch.Draw(_renderTarget, position, Color.Black);
        position.X -= 2;
        position.Y -= 2;
        spriteBatch.Draw(_renderTarget, position, Color.White);
        // text.Draw(spriteBatch, boundaries.TopLeft(), Color.White);
    }
}