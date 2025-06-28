using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FontLoader.ConfigElements;
using FontLoader.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using Velentr.Font;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace FontLoader.Core;

public record struct FontPreview(RenderTarget2D Target, string FontPath, string FontName)
{
    public RenderTarget2D Target = Target;
    public string FontPath = FontPath;
    public string FontName = FontName;
}

internal static class FontPreviewHolder
{
    internal static List<FontPreview> Targets;

    internal static void Load() {
        ModUtilities.SetLoadingText(LocalizationKey.LoadingInstalled);

        Targets = new List<FontPreview>();

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

        var fontFiles = Directory.GetFiles(fontsFolderPath, "*.*", SearchOption.AllDirectories)
            .Where(FontFileTypeChecker.IsFontFile);

        var allFontFiles = fontFiles;

        if (OperatingSystem.IsWindows() && config.FontSearchPath?.Mode is SearchPathMode.SystemAndUser) {
            var localFontsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            localFontsFolderPath = Path.Combine(localFontsFolderPath, @"Microsoft\Windows\Fonts");
            var localFontFiles = Directory.GetFiles(localFontsFolderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(FontFileTypeChecker.IsFontFile);
            allFontFiles = allFontFiles.Concat(localFontFiles);
        }

        if (config.SetupFontPreview) {
            LoadFontWithPreview(allFontFiles);
        }
        else {
            LoadFontWithoutPreview(allFontFiles);
        }
    }

    private static void LoadFontWithoutPreview(IEnumerable<string> allFontFiles) {
        Parallel.ForEach(allFontFiles, fontFile => {
            if (!TryLoadFonts(fontFile, out var font, out string name)) {
                if (font is not null) {
                    Main.RunOnMainThread(() => { font.DisposeFinal(); });
                }

                return;
            }

            Main.RunOnMainThread(() => {
                font.DisposeFinal();
                Targets.Add(new FontPreview(null, fontFile, name));
            });
        });
        // foreach (var fontFile in allFontFiles) {
        //     if (!TryLoadFonts(fontFile, out var font, out string name)) {
        //         font?.DisposeFinal();
        //         continue;
        //     }
        //
        //     font.DisposeFinal();
        //     Targets.Add(new FontPreview(null, fontFile, name));
        // }
    }

    private static void LoadFontWithPreview(IEnumerable<string> allFontFiles) {
        ModUtilities.SetLoadingText(LocalizationKey.SettingPreview);
        var whiten = ModContent.Request<Effect>("FontLoader/Assets/Whiten", AssetRequestMode.ImmediateLoad).Value;
        const int width = 650;
        const int height = 40;
        foreach (var fontFile in allFontFiles) {
            if (!TryLoadFonts(fontFile, out var font, out string name)) {
                font?.DisposeFinal();
                continue;
            }

            Main.RunOnMainThread(() => {
                try {
                    var renderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, width, height, false, default,
                        default, default, RenderTargetUsage.PreserveContents);

                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                        DepthStencilState.None, RasterizerState.CullCounterClockwise, whiten, Matrix.Identity);
                    Main.graphics.GraphicsDevice.SetRenderTarget(renderTarget);
                    Main.graphics.GraphicsDevice.Clear(Color.Transparent);

                    font.Draw(Main.spriteBatch, name, Color.White, new Rectangle(0, 0, width, height));

                    Main.spriteBatch.End();
                    Main.graphics.GraphicsDevice.SetRenderTarget(null);

                    Targets.Add(new FontPreview(renderTarget, fontFile, name));
                }
                catch (ArgumentOutOfRangeException ex) {
                    FontLoader.Instance.Logger.Warn($"Font preview generation failed for '{fontFile}' ({name}): {ex.Message}");
                }
                finally {
                    font.DisposeFinal();
                }
            });

        }
    }

    private static bool TryLoadFonts(string fontFile, out Font font, out string name) {
        var config = ModContent.GetInstance<Config>();
        try {
            font = Statics.Manager.GetMinimalFont(fontFile);
            name = ModUtilities.GetFontName(font);
        }
        catch (Exception e) {
            font = null;
            name = null;
            if (config.DebugText)
                FontLoader.Instance.Logger.Warn($"Failed to load font {fontFile}: {e}");
            return false;
        }

        if (font is null || string.IsNullOrWhiteSpace(name)) {
            if (config.DebugText)
                FontLoader.Instance.Logger.Warn($"Failed to load font {fontFile}");
            return false;
        }

        return true;
    }

    internal static void Unload() {
        Targets = null;
    }
}
