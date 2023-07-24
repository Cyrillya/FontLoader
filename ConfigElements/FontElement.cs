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
    public Font Font { get; set; }
    public string Name { get; }
    public string TypefaceName { get; }

    public FontElement(Font font, string name) {
        Width.Set(0f, 1f);
        Height.Set(32f, 0f);
        Font = font;
        Name = name;
        TypefaceName = font.TypefaceName;
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

        if (!RenderTargetHolder.TargetLookup.TryGetValue(TypefaceName, out var renderTarget) || renderTarget is null) {
            if (Font is null) return;

            Font.MinimalTextureSize = true;

            renderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, settingsWidth, height, false, default,
                default, default, RenderTargetUsage.PreserveContents);

            RenderTargetHolder.AddRequest(Font.TypefaceName, renderTarget, target => {
                var reverseEffect = ModContent.Request<Effect>("FontLoader/Assets/Whiten", AssetRequestMode.ImmediateLoad).Value;

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, reverseEffect, Matrix.Identity);
                Main.graphics.GraphicsDevice.SetRenderTarget(target);
                Main.graphics.GraphicsDevice.Clear(Color.Transparent);

                Font.Draw(spriteBatch, Name, Color.White, new Rectangle(0, 0, settingsWidth, height));

                Main.spriteBatch.End();
                Main.graphics.GraphicsDevice.SetRenderTarget(null);
            });

            return;
        }

        position.X += 10;
        position.Y += 6;
        spriteBatch.Draw(renderTarget, position, Color.Black);
        position.X -= 2;
        position.Y -= 2;
        spriteBatch.Draw(renderTarget, position, Color.White);
        // text.Draw(spriteBatch, boundaries.TopLeft(), Color.White);
    }
}