using System;
using System.Globalization;
using System.Reflection;
using FontLoader.ConfigElements;
using FontLoader.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Chat;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI.Chat;
using Velentr.Font;

namespace FontLoader.Core;

public static class DetourLoader
{
    private static Config _config => ModContent.GetInstance<Config>();
    private static bool _drawingSpecialBorderText;

    private static void SpecialBorderTextPatch(SpriteBatch spriteBatch, FontCollection font, string text, Rectangle boundaries, Color color,
        float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth, int lineSpacing = 0) {
        if (color == Color.Black) {
            return;
        }

        boundaries.X += 2;
        boundaries.Y += 2;
        font.Draw(spriteBatch, text, boundaries, Color.Black.MultiplyRGBA(color), rotation, origin / 2f, scale, effects, layerDepth, lineSpacing);
        boundaries.X -= 2;
        boundaries.Y -= 2;
        font.Draw(spriteBatch, text, boundaries, color, rotation, origin / 2f, scale, effects, layerDepth, lineSpacing);
    }

    private delegate void DsfInternalDrawDelegate(DynamicSpriteFont self, string text, SpriteBatch spriteBatch,
        Vector2 startPosition,
        Color color, float rotation, Vector2 origin, ref Vector2 scale, SpriteEffects spriteEffects, float depth);

    private static void DetourDsfInternalDraw(DsfInternalDrawDelegate orig, DynamicSpriteFont self, string text,
        SpriteBatch spriteBatch, Vector2 position, Color color, float rotation, Vector2 origin, ref Vector2 scale,
        SpriteEffects effects, float depth) {
        if (spriteBatch is null || self is null || text is null) {
            orig(self, text, spriteBatch, position, color, rotation, origin, ref scale, effects, depth);
            return;
        }

        if (false) {
            orig(self, text, spriteBatch, position, color, rotation, origin, ref scale, effects, depth);
            color = Color.CornflowerBlue;
        }

        try {
            var font = self.GetVelentrFont();
            float supersample = ModUtilities.GetRasterSupersample();

            if (font is null || Unloader.Unloading) {
                orig(self, text, spriteBatch, position, color, rotation, origin, ref scale, effects, depth);
                return;
            }

            // TR的字体绘制似乎比正常绘制高一点，这里做个修正
            position.Y -= self.GetYOffset() / supersample;
            var renderScale = scale / supersample;
            var renderOrigin = origin * supersample;
            int lineSpacing = (int) Math.Round(self.LineSpacing * supersample);
            // 别绘制太多文字，不然就会卡死
            var boundaries = new Rectangle((int)(position.X - renderOrigin.X), (int)(position.Y - renderOrigin.Y), Main.screenWidth, Main.screenHeight);

            if (_drawingSpecialBorderText && _config.UseTextShadow) {
                SpecialBorderTextPatch(spriteBatch, font, text, boundaries, color, rotation, renderOrigin, renderScale, effects,
                    depth, lineSpacing);
                return;
            }

            font.Draw(spriteBatch, text, boundaries, color, rotation, renderOrigin, renderScale, effects,
                depth, lineSpacing);
        }
        catch {
            orig(self, text, spriteBatch, position, color, rotation, origin, ref scale, effects, depth);
        }
    }

    private delegate Vector2 DsfMeasureStringDelegate(DynamicSpriteFont self, string text);

    private static Vector2 DetourDsfMeasureString(DsfMeasureStringDelegate orig, DynamicSpriteFont self, string text) {
        if (text is null) {
            return orig(self, text);
        }

        if (!Program.IsMainThread || Unloader.Unloading) {
            return orig(self, text);
        }

        try {
            var font = self.GetVelentrFont();
            if (font is null) {
                return orig(self, text);
            }

            float supersample = ModUtilities.GetRasterSupersample();
            int lineSpacing = (int) Math.Round(self.LineSpacing * supersample);
            return font.MeasureText(text, lineSpacing) / supersample;
        }
        catch {
            return orig(self, text);
        }
    }

    private delegate string DsfCreateWrappedTextDelegate(DynamicSpriteFont self, string text, float maxWidth,
        CultureInfo culture);

    private static string DetourCreateWrappedTextString(DsfCreateWrappedTextDelegate orig, DynamicSpriteFont self,
        string text,
        float maxWidth, CultureInfo culture) {
        if (text is null) {
            return orig(self, text, maxWidth, culture);
        }

        if (!Program.IsMainThread || Unloader.Unloading) {
            return orig(self, text, maxWidth, culture);
        }

        try {
            var font = self.GetVelentrFont();

            if (font is null) {
                return orig(self, text, maxWidth, culture);
            }

            float supersample = ModUtilities.GetRasterSupersample();
            var wrappedTextBuilder = new WrappedTextBuilder(font, maxWidth * supersample, culture);
            wrappedTextBuilder.Append(text);
            return wrappedTextBuilder.ToString();
        }
        catch {
            return orig(self, text, maxWidth, culture);
        }
    }

    public static void Load() {
        ModUtilities.SetLoadingText(LocalizationKey.AddingDetours);

        MonoModHooks.Add(
            typeof(DynamicSpriteFont).GetMethod("InternalDraw", BindingFlags.Instance | BindingFlags.NonPublic),
            DetourDsfInternalDraw);
        MonoModHooks.Add(
            typeof(DynamicSpriteFont).GetMethod("MeasureString", BindingFlags.Instance | BindingFlags.Public),
            DetourDsfMeasureString);
        MonoModHooks.Add(
            typeof(DynamicSpriteFont).GetMethod("CreateWrappedText", BindingFlags.Public | BindingFlags.Instance, null,
                new[] {typeof(string), typeof(float), typeof(CultureInfo)}, null), DetourCreateWrappedTextString);

        On_Utils.DrawBorderStringFourWay += (orig, sb, font, text, x, y, textColor, borderColor, origin, scale) => {
            if (!_config.UseTextShadow) {
                orig.Invoke(sb, font, text, x, y, textColor, borderColor, origin, scale);
                return;
            }

            float spread = 2f * font.GetSpreadMultipiler();
            sb.DrawString(font, text, new Vector2(x + spread, y + spread), borderColor * 0.7f, 0f, origin, scale,
                SpriteEffects.None, 0f);
            sb.DrawString(font, text, new Vector2(x, y), textColor, 0f, origin, scale, SpriteEffects.None, 0f);
        };

        On_Utils.DrawBorderStringBig +=
            (orig, spriteBatch, text, pos, color, scale, anchorx, anchory, maxCharactersDisplayed) => {
                if (!_config.UseTextShadow) {
                    return orig.Invoke(spriteBatch, text, pos, color, scale, anchorx, anchory, maxCharactersDisplayed);
                }

                DynamicSpriteFont value = FontAssets.DeathText.Value;

                TextSnippet[] snippets = ChatManager.ParseMessage(text, color).ToArray();
                ChatManager.ConvertNormalSnippets(snippets);
                Vector2 textSize = ChatManager.GetStringSize(value, snippets, Vector2.One);

                float spread = 2f * value.GetSpreadMultipiler();
                ChatManager.DrawColorCodedString(spriteBatch, value, snippets, pos + new Vector2(spread), Color.Black,
                    0f,
                    new Vector2(anchorx, anchory) * textSize, new Vector2(scale), out var _, maxWidth: -1,
                    ignoreColors: true);

                ChatManager.DrawColorCodedString(spriteBatch, value, snippets, pos, color, 0f,
                    new Vector2(anchorx, anchory) * textSize, new Vector2(scale), out var _, maxWidth: -1);
                return textSize * scale;
            };

        On_ChatManager
                .DrawColorCodedStringShadow_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_float_float +=
            (orig, spriteBatch, font, snippets, position, baseColor, rotation, origin, baseScale, maxWidth, spread) => {
                if (!_config.UseTextShadow) {
                    orig.Invoke(spriteBatch, font, snippets, position, baseColor, rotation, origin, baseScale, maxWidth,
                        spread);
                    return;
                }

                spread *= font.GetSpreadMultipiler();
                ChatManager.DrawColorCodedString(spriteBatch, font, snippets, position + new Vector2(spread), baseColor,
                    rotation, origin, baseScale, out var _, maxWidth, ignoreColors: true);
            };

        On_ChatManager
                .DrawColorCodedStringShadow_SpriteBatch_DynamicSpriteFont_string_Vector2_Color_float_Vector2_Vector2_float_float +=
            (orig, spriteBatch, font, text, position, baseColor, rotation, origin, baseScale, maxWidth, spread) => {
                if (!_config.UseTextShadow) {
                    orig.Invoke(spriteBatch, font, text, position, baseColor, rotation, origin, baseScale, maxWidth,
                        spread);
                    return;
                }

                spread *= font.GetSpreadMultipiler();
                ChatManager.DrawColorCodedString(spriteBatch, font, text, position + new Vector2(spread), baseColor,
                    rotation, origin, baseScale, maxWidth);
            };

        On_Main.DrawInfoAccs += (orig, self) => {
            _drawingSpecialBorderText = true;
            orig.Invoke(self);
            _drawingSpecialBorderText = false;
        };

        // 这个必须留着，不然有些Hook没用，可能是因为内联了，而这边On_再Hook一次并调用原方法就不会内联了
        // On_Main.MouseTextInner += (orig, self, info) => orig.Invoke(self, info);
        On_UIText.InternalSetText += (orig, self, text, scale, large) => orig.Invoke(self, text, scale, large);
        On_UIText.DrawSelf += (orig, self, spriteBatch) => orig.Invoke(self, spriteBatch);
        On_RemadeChatMonitor.DrawChat += (orig, self, chat) => orig.Invoke(self, chat);
        On_GameTipsDisplay.Draw += (orig, self) => orig.Invoke(self);
        //MonoModHooks.Add(
        //    typeof(UIAutoScaleTextTextPanel<LocalizedText>).GetMethod("DrawSelf",
        //        BindingFlags.NonPublic | BindingFlags.Instance),
        //    (Action<object, SpriteBatch> orig, object self, SpriteBatch spriteBatch) => orig.Invoke(self, spriteBatch));
    }
}