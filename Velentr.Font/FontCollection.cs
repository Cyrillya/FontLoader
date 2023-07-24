using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Velentr.Collections.Collections;
using Velentr.Font.Internal;

namespace Velentr.Font;

public class FontCollection : IDisposable
{
    /// <summary>
    /// The manager
    /// </summary>
    private FontManager _manager;

    /// <summary>
    /// The text cache
    /// </summary>
    public Cache<string, Vector2> TextCache = new(Constants.Settings.MaxTextCacheSize);

    /// <summary>
    /// The characters that we currently have generated glyphs for
    /// </summary>
    public Dictionary<char, Internal.Glyph> CharacterGlyphs = new();

    /// <summary>
    /// Caches of glyphs
    /// </summary>
    private readonly List<GlyphCache> _glyphCaches = new List<GlyphCache>();

    public Font MainFont;
    public Font AltFont;
    
    /// <summary>
    /// The calculation cache.
    /// </summary>
    private Dictionary<(string, Rectangle, float, Vector2, Vector2, SpriteEffects, float), List<(Vector2, Glyph)>> _calculationCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FontCollection"/> class.
    /// </summary>
    public FontCollection(FontManager manager, Font mainFont, Font altFont) {
        _manager = manager;
        MainFont = mainFont;
        AltFont = altFont;
        manager.AddFontCollection(this);
    }
    
    public FontCollection(FontManager manager, string mainFontName, byte[] mainFontBytes, string altFontName, byte[] altFontBytes, int size) {
        _manager = manager;
        MainFont = manager.GetFont(mainFontName, mainFontBytes, size);
        AltFont = manager.GetFont(altFontName, altFontBytes, size);
        manager.AddFontCollection(this);
    }
    
    public FontCollection(FontManager manager, string mainFontPath, string altFontPath, int size) {
        _manager = manager;
        MainFont = manager.GetFont(mainFontPath, size);
        AltFont = manager.GetFont(altFontPath, size);
        manager.AddFontCollection(this);
    }

    /// <summary>
    /// Tries the get glyph.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="glyph">The glyph.</param>
    /// <param name="font">The font.</param>
    /// <returns></returns>
    internal bool TryGetGlyph(char character, out Glyph glyph, out Font font) {
        if (!CharacterGlyphs.TryGetValue(character, out glyph))
        {
            glyph = GenerateGlyph(character);
            CharacterGlyphs.Add(character, glyph);
        }

        font = glyph.Font;
        return true;
    }

    /// <summary>
    /// Generates the glyph.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <returns></returns>
    /// <exception cref="Exception">Could not generate character [{character}]!</exception>
    public Glyph GenerateGlyph(char character)
    {
        var cache = _glyphCaches.FirstOrDefault(c => !c.Full);
        if (cache == null)
        {
            cache = new GlyphCache(MainFont, _manager);
            _glyphCaches.Add(cache);
        }

        if (!cache.AddCharacterToCache(character, out var cachedGlyph))
        {
            cache = new GlyphCache(MainFont, _manager);
            _glyphCaches.Add(cache);
            if (!cache.AddCharacterToCache(character, out cachedGlyph))
            {
                throw new Exception($"Could not generate character [{character}]!");
            }
        }

        return cachedGlyph;
    }

    /// <summary>
    /// Draws the text to the screen at the specified position and with the specified color.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch.</param>
    /// <param name="text">The text.</param>
    /// <param name="boundaries">The boundaries.</param>
    /// <param name="color">The color.</param>
    /// <param name="lineSpacing">The line spacing.</param>
    public void Draw(SpriteBatch spriteBatch, string text, Rectangle boundaries, Color color, int lineSpacing = 0) {
        var key = (text, boundaries, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, lineSpacing);
        if (_calculationCache.TryGetValue(key, out var items)) {
            foreach (var (position, character) in items) {
                float rotation = key.Item3;
                var origin = key.Item4;
                var scale = key.Item5;
                spriteBatch.Draw(character.GlyphCache.Texture, position, character.Boundary, color, rotation, origin, scale, SpriteEffects.None, 0f);
            }

            return;
        }

        var warpLine = boundaries.Width > 0;
        var offsetX = 0;
        var offsetY = 0;

        var width = warpLine ? boundaries.Width : spriteBatch.GraphicsDevice.Viewport.Width;
        var height = boundaries.Height > 0 ? boundaries.Height : spriteBatch.GraphicsDevice.Viewport.Height;

        var countX = 0;
        var underrun = 0;
        var finalCharacterIndex = text.Length - 1;

        var currentColor = color;
        var infos = new List<(Vector2, Glyph)>();
        for (var i = 0; i < text.Length; i++) {
            TryGetGlyph(text[i], out var cachedCharacter, out var font);
            lineSpacing = lineSpacing is 0 ? cachedCharacter.AdvanceY : lineSpacing;

            if (warpLine && offsetX + cachedCharacter.Boundary.Width + countX > width || text[i] == '\n') {
                offsetX = 0;
                underrun = 0;
                offsetY += lineSpacing;
            }

            if (text[i] == '\r' || text[i] == '\n') {
                continue;
            }

            if (offsetY > height || !warpLine && offsetX > width) {
                break;
            }

            // calculate underrun
            underrun += -cachedCharacter.BearingX;
            if (offsetX == 0) {
                offsetX += underrun;
            }

            if (underrun <= 0) {
                underrun = 0;
            }

            var position = new Vector2(boundaries.X + offsetX, boundaries.Y + offsetY);

            infos.Add((position, cachedCharacter));

            spriteBatch.Draw(cachedCharacter.GlyphCache.Texture, position, cachedCharacter.Boundary, currentColor, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
            offsetX += cachedCharacter.Boundary.Width;

            // calculate kerning
            if (i != finalCharacterIndex) {
                var nextCharacter = text[i + 1];
                if (TryGetGlyph(nextCharacter, out var nextCachedCharacter, out _)) {
                    var kerning = font.GetKerning(cachedCharacter, nextCachedCharacter);
                    var maxBounds = cachedCharacter.AdvanceX * Constants.Settings.KerningSanityMultiplier;
                    if (kerning <= maxBounds && kerning >= -maxBounds) {
                        offsetX += kerning;
                    }
                }
            }
        }

        _calculationCache.Add(key, infos);
    }
    
    /// <summary>
    /// Draws the text to the screen at the specified position and with the specified color.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch.</param>
    /// <param name="text">The text.</param>
    /// <param name="position">The position.</param>
    /// <param name="color">The color.</param>
    /// <param name="lineSpacing">The line spacing.</param>
    public void Draw(SpriteBatch spriteBatch, string text, Vector2 position, Color color, int lineSpacing = 0) =>
        Draw(spriteBatch, text, new Rectangle((int) position.X, (int) position.Y, 9999999, 9999999), color, lineSpacing);

    /// <summary>
    /// Draws the text to the screen at the specified position and with the specified color.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch.</param>
    /// <param name="text">The text.</param>
    /// <param name="boundaries">The boundaries.</param>
    /// <param name="color">The color.</param>
    /// <param name="rotation">The rotation.</param>
    /// <param name="origin">The origin.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="effects">The effects.</param>
    /// <param name="layerDepth">The layer depth.</param>
    /// <param name="lineSpacing">The line spacing.</param>
    public void Draw(SpriteBatch spriteBatch, string text, Rectangle boundaries, Color color, float rotation,
        Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth, int lineSpacing = 0) {
        var key = (text, boundaries, rotation, origin, scale, effects, lineSpacing);
        if (_calculationCache.TryGetValue(key, out var items)) {
            foreach (var (position, character) in items) {
                spriteBatch.Draw(character.GlyphCache.Texture, position, character.Boundary, color, rotation, origin, scale, effects, 0f);
            }

            return;
        }

        // calculate the rest of the text position
        var warpLine = boundaries.Width > 0;
        var offsetX = 0;
        var offsetY = 0;

        var width = warpLine ? boundaries.Width : spriteBatch.GraphicsDevice.Viewport.Width;
        var height = boundaries.Height > 0 ? boundaries.Height : spriteBatch.GraphicsDevice.Viewport.Height;

        var countX = 0;
        var underrun = 0;
        var finalCharacterIndex = text.Length - 1;

        var currentColor = color;
        var infos = new List<(Vector2, Glyph)>();
        for (var i = 0; i < text.Length; i++) {
            TryGetGlyph(text[i], out var cachedCharacter, out var font);
            lineSpacing = lineSpacing is 0 ? cachedCharacter.AdvanceY : lineSpacing;

            // calculate our transformation matrix
            var flipAdjustment = Vector2.Zero;
            var flippedVertically = effects.HasFlag(SpriteEffects.FlipVertically);
            var flippedHorizontally = effects.HasFlag(SpriteEffects.FlipHorizontally);

            // if we've flipped, handle adjusting our location as required
            if (flippedVertically || flippedHorizontally) {
                var size = font.MeasureText(text);

                if (flippedHorizontally) {
                    origin.X *= -1;
                    flipAdjustment.X -= size.X;
                }

                if (flippedVertically) {
                    origin.Y *= -1;
                    flipAdjustment.Y = font.GlyphHeight - size.Y;
                }
            }

            // Handle our rotation as required
            var transformation = Matrix.Identity;
            float cos, sin = 0;
            var xScale = flippedHorizontally ? -scale.X : scale.X;
            var yScale = flippedVertically ? -scale.Y : scale.Y;
            var xOrigin = flipAdjustment.X - origin.X;
            var yOrigin = flipAdjustment.Y - origin.Y;
            if (Helpers.FloatsAreEqual(rotation, 0) || Helpers.FloatsAreEqual(rotation / Constants.TWO_PI, 1)) {
                transformation.M11 = xScale;
                transformation.M22 = yScale;
                transformation.M41 = xOrigin * transformation.M11 + boundaries.X;
                transformation.M42 = yOrigin * transformation.M22 + boundaries.Y;
            }
            else {
                cos = (float) Math.Cos(rotation);
                sin = (float) Math.Sin(rotation);
                transformation.M11 = xScale * cos;
                transformation.M12 = xScale * sin;
                transformation.M21 = yScale * -sin;
                transformation.M22 = yScale * cos;
                transformation.M41 = (xOrigin * transformation.M11 + yOrigin * transformation.M21) + boundaries.X;
                transformation.M42 = (xOrigin * transformation.M12 + yOrigin * transformation.M22) + boundaries.Y;
            }

            if (warpLine && offsetX + cachedCharacter.Boundary.Width + countX > width || text[i] == '\n') {
                offsetX = 0;
                underrun = 0;
                offsetY += lineSpacing;
            }

            if (text[i] == '\r' || text[i] == '\n') {
                continue;
            }

            if (offsetY > height || !warpLine && offsetX > width) {
                break;
            }

            // calculate underrun
            underrun += -cachedCharacter.BearingX;
            if (offsetX == 0) {
                offsetX += underrun;
            }

            if (underrun <= 0) {
                underrun = 0;
            }

            var characterPosition = new Vector2(offsetX, offsetY);
            Vector2.Transform(ref characterPosition, ref transformation, out characterPosition);

            infos.Add((characterPosition, cachedCharacter));

            spriteBatch.Draw(cachedCharacter.GlyphCache.Texture, characterPosition, cachedCharacter.Boundary,
                currentColor, rotation, origin, scale, effects, layerDepth);
            offsetX += cachedCharacter.Boundary.Width;

            // calculate kerning
            if (i != finalCharacterIndex) {
                var nextCharacter = text[i + 1];
                if (TryGetGlyph(nextCharacter, out var nextCachedCharacter, out _)) {
                    var kerning = font.GetKerning(cachedCharacter, nextCachedCharacter);
                    var maxBounds = cachedCharacter.AdvanceX * Constants.Settings.KerningSanityMultiplier;
                    if (kerning <= maxBounds && kerning >= -maxBounds) {
                        offsetX += kerning;
                    }
                }
            }
        }

        _calculationCache.Add(key, infos);
    }

    /// <summary>
    /// Draws the text to the screen at the specified position and with the specified color.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch.</param>
    /// <param name="text">The text.</param>
    /// <param name="position">The position.</param>
    /// <param name="color">The color.</param>
    /// <param name="rotation">The rotation.</param>
    /// <param name="origin">The origin.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="effects">The effects.</param>
    /// <param name="layerDepth">The layer depth.</param>
    /// <param name="lineSpacing">The line spacing.</param>
    public void Draw(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float rotation,
        Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth, int lineSpacing = 0) =>
        Draw(spriteBatch, text, new Rectangle((int) position.X, (int) position.Y, 9999999, 9999999), color, rotation,
            origin, scale, effects, layerDepth, lineSpacing);

    /// <summary>
    /// Measures the text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The size of the text.</returns>
    public Vector2 MeasureText(StringBuilder text, int lineSpacing = 0) {
        return MeasureText(text.ToString(), lineSpacing);
    }

    /// <summary>
    /// Measures the text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The size of the text.</returns>
    public Vector2 MeasureText(string text, int lineSpacing = 0) {
        // exit early if we've got the text in cache from making a Text object previously...
        if (TextCache.TryGetItem(text, out var size)) {
            return size;
        }

        // otherwise, we need to calculate the size of the string...
        var finalSize = new Vector2(0, 0);

        var offsetX = 0;
        var offsetY = 0;

        var underrun = 0;
        var finalCharacterIndex = text.Length - 1;

        for (var i = 0; i < text.Length; i++) {
            TryGetGlyph(text[i], out var cachedCharacter, out var font);
            lineSpacing = lineSpacing is 0 ? cachedCharacter.AdvanceY : lineSpacing;
            if (i == 0) {
                finalSize.Y += lineSpacing;
            }

            if (text[i] == '\n') {
                finalSize.X = Math.Max(offsetX, finalSize.X);
                offsetX = 0;
                underrun = 0;
                offsetY += lineSpacing;
                if (i != finalCharacterIndex) {
                    finalSize.Y += lineSpacing;
                }
            }

            if (text[i] == '\r' || text[i] == '\n') {
                continue;
            }

            // calculate underrun
            underrun += -cachedCharacter.BearingX;
            if (offsetX == 0) {
                offsetX += underrun;
            }

            if (underrun <= 0) {
                underrun = 0;
            }

            offsetX += cachedCharacter.Boundary.Width;

            // calculate kerning
            if (i != finalCharacterIndex) {
                var nextCharacter = text[i + 1];
                if (TryGetGlyph(nextCharacter, out var nextCachedCharacter, out _)) {
                    var kerning = font.GetKerning(cachedCharacter, nextCachedCharacter);
                    var maxBounds = cachedCharacter.AdvanceX * Constants.Settings.KerningSanityMultiplier;
                    if (kerning <= maxBounds && kerning >= -maxBounds) {
                        offsetX += kerning;
                    }
                }
            }
            else {
                finalSize.X = Math.Max(offsetX, finalSize.X);
            }
        }

        TextCache.AddItemToCache(text, finalSize);

        return finalSize;
    }

    public void Dispose() {
        TextCache.Clear();
        _calculationCache.Clear();
        _glyphCaches.Clear();
        CharacterGlyphs.Clear();
    }
}