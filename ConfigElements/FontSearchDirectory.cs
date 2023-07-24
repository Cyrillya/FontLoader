using System;
using System.Drawing.Text;
using System.Linq;
using FontLoader.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace FontLoader.ConfigElements;

public class FontSearchDirectory
{
    public SearchPathMode Mode;
    public string CustomPath;

    public FontSearchDirectory() {
        Mode = SearchPathMode.SystemAndUser;
        CustomPath = "";
    }

    public override bool Equals(object obj) {
        if (obj is FontSearchDirectory other)
            return Mode == other.Mode && CustomPath == other.CustomPath;
        return base.Equals(obj);
    }

    public override int GetHashCode() {
        return new {Mode, CustomPath}.GetHashCode();
    }
}

public class FontSearchDirectoryElement : ConfigElement<FontSearchDirectory>
{
    internal class BooleanElementInner : ConfigElement
    {
        private Func<bool> _getValue;
        private Asset<Texture2D> _toggleTexture;

        public BooleanElementInner(SearchPathMode mode, Func<bool> getValue) {
            _toggleTexture = Main.Assets.Request<Texture2D>("Images/UI/Settings_Toggle");
            _getValue = getValue;

            TextDisplayFunction = () => GetNameLocalized(mode);
            TooltipFunction = () => GetTooltipLocalized(mode);

            Height.Set(36f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch) {
            var dimensions = GetDimensions();
            int settingsWidth = (int) Math.Ceiling(dimensions.Width - 4f);
            int height = (int) Math.Ceiling(dimensions.Height);
            var backgroundColor = UICommon.DefaultUIBlue;
            var panelColor = IsMouseHovering ? backgroundColor : backgroundColor.MultiplyRGBA(new Color(180, 180, 180));
            var position = dimensions.Position();
            DrawPanel2(spriteBatch, position, TextureAssets.SettingsPanel.Value, settingsWidth, height, panelColor);

            position.X += 8f;
            position.Y += 8f;
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, TextDisplayFunction(),
                position, Color.White, 0.0f, Vector2.Zero, Vector2.One);

            bool value = _getValue();

            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value,
                value ? Lang.menu[126].Value : Lang.menu[124].Value,
                new Vector2(dimensions.X + dimensions.Width - 60, dimensions.Y + 8f), Color.White, 0f, Vector2.Zero,
                Vector2.One);

            var sourceRectangle = new Rectangle(value ? (_toggleTexture.Width() - 2) / 2 + 2 : 0, 0,
                (_toggleTexture.Width() - 2) / 2, _toggleTexture.Height());
            var drawPosition = new Vector2(dimensions.X + dimensions.Width - sourceRectangle.Width - 10f,
                dimensions.Y + 8f);
            spriteBatch.Draw(_toggleTexture.Value, drawPosition, sourceRectangle, Color.White, 0f, Vector2.Zero,
                Vector2.One, SpriteEffects.None, 0f);

            if (IsMouseHovering) {
                UICommon.TooltipMouseText(TooltipFunction());
            }
        }
    }

    private static string GetNameLocalized(SearchPathMode mode) =>
        Language.GetTextValue(
            FontLoader.Instance.GetLocalizationKey($"Configs.SearchPathMode.{mode.ToString()}.Label"));

    private static string GetTooltipLocalized(SearchPathMode mode) =>
        Language.GetTextValue(
            FontLoader.Instance.GetLocalizationKey($"Configs.SearchPathMode.{mode.ToString()}.Tooltip"));

    public override void OnBind() {
        base.OnBind();

        DrawLabel = false;
        Value ??= new FontSearchDirectory();
        Height.Set(194f, 0f);

        var labelText = new UIText(Label, textScale: 0.9f) {
            Top = {Pixels = 12},
            Left = {Pixels = 10}
        };
        Append(labelText);

        var fontNameText = new UIText(GetNameLocalized(Value.Mode), textScale: 0.9f) {
            Top = {Pixels = 12},
            Left = {Pixels = -10},
            Width = {Precent = 1f},
            TextOriginX = 1f
        };
        fontNameText.OnUpdate += _ => { fontNameText.SetText(GetNameLocalized(Value.Mode)); };
        Append(fontNameText);

        var systemAndUserButton =
            new BooleanElementInner(SearchPathMode.SystemAndUser, () => Value.Mode is SearchPathMode.SystemAndUser) {
                Top = {Pixels = 34},
                Left = {Pixels = 10},
                Width = {Pixels = -16, Precent = 1f}
            };
        systemAndUserButton.OnLeftClick += (_, _) => {
            Value.Mode = SearchPathMode.SystemAndUser;
            SetObject(Value);
        };
        Append(systemAndUserButton);

        var systemButton =
            new BooleanElementInner(SearchPathMode.SystemOnly, () => Value.Mode is SearchPathMode.SystemOnly) {
                Top = {Pixels = 74},
                Left = {Pixels = 10},
                Width = {Pixels = -16, Precent = 1f}
            };
        systemButton.OnLeftClick += (_, _) => {
            Value.Mode = SearchPathMode.SystemOnly;
            SetObject(Value);
        };
        Append(systemButton);

        var customButton =
            new BooleanElementInner(SearchPathMode.CustomPath, () => Value.Mode is SearchPathMode.CustomPath) {
                Top = {Pixels = 114},
                Left = {Pixels = 10},
                Width = {Pixels = -16, Precent = 1f},
                Height = {Pixels = 70}
            };
        customButton.OnLeftClick += (_, _) => {
            Value.Mode = SearchPathMode.CustomPath;
            SetObject(Value);
        };

        var textBoxBackground = new UIPanel {
            PaddingTop = 0,
            PaddingBottom = 0,
            PaddingLeft = 0,
            PaddingRight = 0,
            BackgroundColor = Color.Transparent,
            BorderColor = Color.Transparent,
            Width = {Pixels = -16f, Percent = 1f},
            Height = {Pixels = 30},
            Top = {Pixels = 38f},
            Left = {Pixels = 6f}
        };
        textBoxBackground.Append(new UIHorizontalSeparator {
            Top = new StyleDimension(-8f, 1f),
            Width = StyleDimension.FromPercent(1f),
            Color = Color.Lerp(Color.White, new Color(63, 65, 151, 255), 0.85f) * 0.9f
        });
        var customFolderInput = new UIFocusInputTextField("Type folder name") {
            CurrentString = Value.CustomPath ?? "",
            Width = StyleDimension.Fill,
            Height = {Pixels = -6f, Percent = 1f},
            Top = {Pixels = 2f}
        };
        customFolderInput.OnTextChange += (_, _) => {
            Value.CustomPath = customFolderInput.CurrentString;
            SetObject(Value);
        };
        customFolderInput.OnRightClick += (_, _) => customFolderInput.SetText("");
        textBoxBackground.Append(customFolderInput);
        customButton.Append(textBoxBackground);

        Append(customButton);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        // TML的Tooltip设置遇到多层就傻了，不会处理，所以这里手动处理
        var tooltipFunction = TooltipFunction;
        TooltipFunction = null;

        base.DrawSelf(spriteBatch);

        TooltipFunction = tooltipFunction;
        if (IsMouseHovering) {
            UICommon.TooltipMouseText(TooltipFunction());
        }
    }
}