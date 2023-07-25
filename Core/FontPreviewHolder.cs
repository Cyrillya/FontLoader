using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FontLoader.ConfigElements;
using FontLoader.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace FontLoader.Core;

public record struct FontPreview(RenderTarget2D Target, string FontPath, string FontName)
{
    public RenderTarget2D Target = Target;
    public string FontPath = FontPath;
    public string FontName = FontName;
}

internal static class FontPreviewHolder
{
    internal static List<FontPreview> TargetLookup;

    internal static void Load() {
        ModUtilities.SetLoadingText(LocalizationKey.LoadingInstalled);

        TargetLookup = new List<FontPreview>();

        var config = ModContent.GetInstance<Config>();

        var fontsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        if (config.FontSearchPath?.Mode is SearchPathMode.CustomPath) {
            string customPath = config.FontSearchPath?.CustomPath;
            if (Directory.Exists(customPath)) {
                fontsFolderPath = customPath;
            }
            else {
                return;
            }
        }
        // fontsFolderPath = @"F:\Downloads\Compressed\苹方字体19.0d3e2版本_2";

        var fontFiles = Directory.GetFiles(fontsFolderPath, "*.*", SearchOption.AllDirectories)
            .Where(ModUtilities.IsTtfOrOtfFile);

        var allFontFiles = fontFiles;

        if (OperatingSystem.IsWindows() && config.FontSearchPath?.Mode is SearchPathMode.SystemAndUser) {
            var localFontsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            localFontsFolderPath = Path.Combine(localFontsFolderPath, @"Microsoft\Windows\Fonts");
            var localFontFiles = Directory.GetFiles(localFontsFolderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(ModUtilities.IsTtfOrOtfFile);
            allFontFiles = allFontFiles.Concat(localFontFiles);
        }

        var whiten = ModContent.Request<Effect>("FontLoader/Assets/Whiten", AssetRequestMode.ImmediateLoad).Value;
        const int width = 650;
        const int height = 40;
        foreach (var fontFile in allFontFiles) {
            var font = Statics.Manager.GetMinimalFont(fontFile);
            string name = ModUtilities.GetFontName(font);

            Main.RunOnMainThread(() => {
                var renderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, width, height, false, default,
                    default, default, RenderTargetUsage.PreserveContents);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, whiten, Matrix.Identity);
                Main.graphics.GraphicsDevice.SetRenderTarget(renderTarget);
                Main.graphics.GraphicsDevice.Clear(Color.Transparent);

                font.Draw(Main.spriteBatch, name, Color.White, new Rectangle(0, 0, width, height));

                Main.spriteBatch.End();
                Main.graphics.GraphicsDevice.SetRenderTarget(null);

                font.DisposeFinal();

                TargetLookup.Add(new FontPreview(renderTarget, fontFile, name));
            });
        }
    }

    internal static void Unload() {
        TargetLookup = null;
    }
}