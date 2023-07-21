#define UsePublicizer

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using FontLoader.Core;
using FontLoader.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;
using Velentr.Font;

namespace FontLoader;

public class FontLoader : Mod
{
    private delegate void DsfInternalDrawDelegate(DynamicSpriteFont self, string text, SpriteBatch spriteBatch,
        Vector2 startPosition,
        Color color, float rotation, Vector2 origin, ref Vector2 scale, SpriteEffects spriteEffects, float depth);

    private void DetourDsfInternalDraw(DsfInternalDrawDelegate orig, DynamicSpriteFont self, string text,
        SpriteBatch spriteBatch,
        Vector2 position, Color color, float rotation, Vector2 origin, ref Vector2 scale,
        SpriteEffects effects, float depth) {
        if (false) {
            orig(self, text, spriteBatch, position, color, rotation, origin, ref scale, effects, depth);
            color = Color.CornflowerBlue;
        }

        var font = self.GetVelentrFont();

        if (font is null || _unloading) {
            orig(self, text, spriteBatch, position, color, rotation, origin, ref scale, effects, depth);
            return;
        }

        // TR的字体绘制似乎比正常绘制高一点，这里做个修正
        position.Y -= font.Size * 0.09f;
        // 别绘制太多文字，不然就会卡死
        var boundaries = new Rectangle((int) position.X, (int) position.Y, Main.screenWidth, Main.screenHeight);

        font.Draw(Main.spriteBatch, text, boundaries, color, rotation, origin / 2f, scale, effects, depth,
            self.LineSpacing);
    }

    private delegate Vector2 DsfMeasureStringDelegate(DynamicSpriteFont self, string text);

    private Vector2 DetourDsfMeasureString(DsfMeasureStringDelegate orig, DynamicSpriteFont self, string text) {
        if (!Program.IsMainThread || _vanillaMeasureString || _unloading) {
            return orig(self, text);
        }

        var font = self.GetVelentrFont();
        return font?.MeasureText(text, self.LineSpacing) ?? orig(self, text);
    }

    private delegate string DsfCreateWrappedTextDelegate(DynamicSpriteFont self, string text, float maxWidth,
        CultureInfo culture);

    private string DetourCreateWrappedTextString(DsfCreateWrappedTextDelegate orig, DynamicSpriteFont self, string text,
        float maxWidth, CultureInfo culture) {
        if (!Program.IsMainThread || _unloading) {
            return orig(self, text, maxWidth, culture);
        }

        var font = self.GetVelentrFont();

        if (font is null) {
            return orig(self, text, maxWidth, culture);
        }

        var wrappedTextBuilder = new WrappedTextBuilder(font, maxWidth, culture);
        wrappedTextBuilder.Append(text);
        return wrappedTextBuilder.ToString();
    }

    private static bool _vanillaMeasureString;
    private static bool _unloading;
    private static FontManager _manager;
    internal static FontCollection FontDeathText;
    internal static FontCollection FontMouseText;
    internal static FontCollection FontCombatText;
    internal static FontCollection FontCombatCrit;
    internal static FontCollection FontItemStack;

    public override void Load() {
        string targetFilePath = AppDomain.CurrentDomain.BaseDirectory; // 保存目标路径
        const string targetFileName = "freetype6.dll"; // 保存目标文件名

        string fullPath = Path.Combine(targetFilePath, targetFileName);

        try {
            using var fileStream = GetFileStream(targetFileName);
            using var targetStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);

            fileStream.CopyTo(targetStream);

            Console.WriteLine(Language.GetTextValue(GetLocalizationKey("FreeTypeSaved")) + targetFilePath);
        }
        catch (Exception ex) {
            Logger.Warn(Language.GetTextValue(GetLocalizationKey("FreeTypeSaveError")), ex);
        }

        NativeLibrary.Load(fullPath);

        var pingfangPath = @"F:\Downloads\Compressed\苹方字体19.0d3e2版本_2\苹方-简 中粗体.otf";
        var andybPath = @"D:\Program Files (x86)\Epic Games\Launcher\Engine\Content\Slate\Fonts\Roboto-Bold.ttf";
        // var fontPath = @"C:\Users\Administrator\AppData\Local\Microsoft\FontCache\4\CloudFonts\Microsoft YaHei\47849284094.ttf";
        _manager = new FontManager(Main.instance.GraphicsDevice);
        FontDeathText = new FontCollection(_manager, (andybPath, 42, 0, 255), (pingfangPath, 48, 0, 9999999));
        FontMouseText = new FontCollection(_manager, (andybPath, 17, 0, 255), (pingfangPath, 19, 0, 9999999));
        FontCombatText = new FontCollection(_manager, (andybPath, 18, 0, 255), (pingfangPath, 20, 0, 9999999));
        FontCombatCrit = new FontCollection(_manager, (andybPath, 24, 0, 255), (pingfangPath, 25, 0, 9999999));
        FontItemStack = new FontCollection(_manager, (andybPath, 15, 0, 255), (pingfangPath, 15, 0, 9999999));

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
            float spread = 2f * font.GetSpreadMultipiler();
            sb.DrawString(font, text, new Vector2(x + spread, y + spread), borderColor * 0.7f, 0f, origin, scale,
                SpriteEffects.None, 0f);
            sb.DrawString(font, text, new Vector2(x, y), textColor, 0f, origin, scale, SpriteEffects.None, 0f);
        };

        On_Utils.DrawBorderStringBig +=
            (orig, spriteBatch, text, pos, color, scale, anchorx, anchory, maxCharactersDisplayed) => {
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
                spread *= font.GetSpreadMultipiler();
                ChatManager.DrawColorCodedString(spriteBatch, font, snippets, position + new Vector2(spread), baseColor,
                    rotation, origin, baseScale, out var _, maxWidth, ignoreColors: true);
            };

        On_ChatManager
                .DrawColorCodedStringShadow_SpriteBatch_DynamicSpriteFont_string_Vector2_Color_float_Vector2_Vector2_float_float +=
            (orig, spriteBatch, font, text, position, baseColor, rotation, origin, baseScale, maxWidth, spread) => {
                spread *= font.GetSpreadMultipiler();
                ChatManager.DrawColorCodedString(spriteBatch, font, text, position + new Vector2(spread), baseColor,
                    rotation, origin, baseScale, maxWidth);
            };

        // 这个必须留着，不然UIText中运行CreateWrappedText时，Hook没用，也不知道为什么
        On_UIText.InternalSetText += (orig, self, text, scale, large) => orig.Invoke(self, text, scale, large);

#if UsePublicizer
        // 虽然这就是把源码复制了一遍，但是不知道为啥不这样的话我DrawColorCodedStringShadow的Hook就没法应用到这上面
        On_UIText.DrawSelf += (orig, self, spriteBatch) => {
            self.VerifyTextState();
            CalculatedStyle innerDimensions = self.GetInnerDimensions();
            Vector2 position = innerDimensions.Position();
            if (self._isLarge)
                position.Y -= 10f * self._textScale;
            else
                position.Y -= 2f * self._textScale;

            position.X += (innerDimensions.Width - self._textSize.X) * self.TextOriginX;
            position.Y += (innerDimensions.Height - self._textSize.Y) * self.TextOriginY;
            float num = self._textScale;
            if (self.DynamicallyScaleDownToWidth && self._textSize.X > innerDimensions.Width)
                num *= innerDimensions.Width / self._textSize.X;

            DynamicSpriteFont value = (self._isLarge ? FontAssets.DeathText : FontAssets.MouseText).Value;
            Vector2 vector = value.MeasureString(self._visibleText);
            Color baseColor = self._shadowColor * ((float)(int)self._color.A / 255f);
            Vector2 origin = new Vector2(0f, 0f) * vector;
            Vector2 baseScale = new Vector2(num);
            TextSnippet[] snippets = ChatManager.ParseMessage(self._visibleText, self._color).ToArray();
            ChatManager.ConvertNormalSnippets(snippets);
            ChatManager.DrawColorCodedStringShadow(spriteBatch, value, snippets, position, baseColor, 0f, origin, baseScale, -1f, 1.5f);
            ChatManager.DrawColorCodedString(spriteBatch, value, snippets, position, Color.White, 0f, origin, baseScale, out var _, -1f);
        };
        
        MonoModHooks.Add(
            typeof(UIAutoScaleTextTextPanel<LocalizedText>).GetMethod("DrawSelf", BindingFlags.NonPublic | BindingFlags.Instance),
            (Action<object, SpriteBatch> orig, object self, SpriteBatch spriteBatch) => {
                if (Program.IsMainThread || self is not UIAutoScaleTextTextPanel<LocalizedText> textPanel) {
                    orig.Invoke(self, spriteBatch);
                    return;
                }

                if (float.IsNaN(textPanel.TextScale))
                    textPanel.Recalculate();

                if (textPanel.DrawPanel) {
                    if (textPanel._needsTextureLoading) {
                        textPanel._needsTextureLoading = false;
                        textPanel.LoadTextures();
                    }

                    if (textPanel._backgroundTexture != null)
                        textPanel.DrawPanel(spriteBatch, textPanel._backgroundTexture.Value, textPanel.BackgroundColor);

                    if (textPanel._borderTexture != null)
                        textPanel.DrawPanel(spriteBatch, textPanel._borderTexture.Value, textPanel.BorderColor);
                }

                var innerDimensions = textPanel.GetDimensions().ToRectangle();
                innerDimensions.Inflate(-4, 0);
                innerDimensions.Y += 8;
                innerDimensions.Height -= 8;
                for (int i = 0; i < textPanel.textStrings.Length; i++) {
                    //Vector2 pos = innerDimensions.Center.ToVector2() + drawOffsets[i];
                    Vector2 pos = innerDimensions.TopLeft() + textPanel.drawOffsets[i];
                    if (textPanel.IsLarge)
                        Utils.DrawBorderStringBig(spriteBatch, textPanel.textStrings[i], pos, textPanel.TextColor, textPanel.TextScale, 0f, 0f, -1);
                    else
                        Utils.DrawBorderString(spriteBatch, textPanel.textStrings[i], pos, textPanel.TextColor, textPanel.TextScale, 0f, 0f, -1);
                }
            });
#endif

        // Main.OnPostDraw += DrawingTest;
    }

    public override void Unload() {
        // Main.OnPostDraw -= DrawingTest;
        _unloading = true;
        _manager.Dispose();
        FontDeathText.Dispose();
        FontMouseText.Dispose();
        FontCombatText.Dispose();
        FontCombatCrit.Dispose();
        FontItemStack.Dispose();
        FontDeathText = null;
        FontMouseText = null;
        FontCombatText = null;
        FontCombatCrit = null;
        FontItemStack = null;
        _unloading = false;
    }

    private void DrawingTest(GameTime gameTime) {
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

        var damnText = "Hello World! \n中国智造，慧及全球";
        FontDeathText.Draw(Main.spriteBatch, damnText, new Vector2(50, 50), Color.White);
        var wtf =
            @"You are protected, in short, by your ability to love! The only protection that can possibly work against the lure of power like Voldemort's! In spite of all the temptation you have endured, all the suffering, you remain pure of heart, just as pure as you were at the age of eleven, when you stared into a mirror that reflected your heart's desire, and it showed you only the way to thwart Lord Voldemort, and not immortality or riches. Harry, have you any idea how few wizards could have seen what you saw in that mirror?";
        var wrappedText = FontAssets.MouseText.Value.CreateWrappedText(wtf, 900);
        Main.spriteBatch.DrawString(FontAssets.MouseText.Value, wrappedText, new Vector2(100, 500), Color.White);

        Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.DeathText.Value, damnText, 100, 200, Color.White,
            Color.Black, Vector2.Zero, 1f);

        Main.spriteBatch.End();
    }
}