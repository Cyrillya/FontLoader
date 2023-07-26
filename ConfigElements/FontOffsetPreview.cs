using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI.Chat;

namespace FontLoader.ConfigElements;

public class FontOffsetPreview : FloatElement
{
    private Asset<Texture2D> _texture;
    protected Asset<DynamicSpriteFont> Font;
    protected float BaseOffset; // 基准偏移，用来让默认字体在默认值下贴合边缘
    protected string TextKey;

    public override void OnBind() {
        base.OnBind();
        Height.Set(70f, 0f);
        BaseOffset = 6f;
        Font = FontAssets.MouseText;
        TextKey = "Configs.Config.GeneralFontOffsetY.Text";
        _texture = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/Separator1");
    }

    public virtual float GetAppliedOffset() =>
        ModContent.GetInstance<Config>().GeneralFontOffsetY;

    public override void Draw(SpriteBatch spriteBatch) {
        base.Draw(spriteBatch);

        var dimensions = GetDimensions();
        var linePosition = dimensions.Position() + new Vector2(6f, dimensions.Height - 14f);
        var text = Language.GetOrRegister(FontLoader.Instance.GetLocalizationKey(TextKey));
        var textPosition = linePosition;
        textPosition.X += 4f;
        textPosition.Y -= Font.Value.MeasureString(text.Value).Y - BaseOffset;
        textPosition.Y -= (float) GetObject();
        textPosition.Y += GetAppliedOffset();
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, Font.Value, text.Value, textPosition, Color.White, 0f,
            Vector2.Zero, Vector2.One, spread: 1.2f);
        Utils.DrawPanel(_texture.Value, 2, 0, spriteBatch, linePosition, dimensions.Width - 12f, Color.White);
    }
}

public class BigFontOffsetPreview : FontOffsetPreview
{
    public override void OnBind() {
        base.OnBind();
        Height.Set(94f, 0f);
        Font = FontAssets.DeathText;
        BaseOffset = 24f;
        TextKey = "Configs.Config.BigFontOffsetY.Text";
    }

    public override float GetAppliedOffset() =>
        ModContent.GetInstance<Config>().BigFontOffsetY;
}