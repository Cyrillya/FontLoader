﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpFont;

namespace Velentr.Font.Internal
{
    /// <summary>
    /// A cache of Glyphs.
    /// </summary>
    public class GlyphCache
    {
        /// <summary>
        /// The width of the GlyphCache.
        /// </summary>
        public static int Width = Constants.DEFAULT_REACH_TEXTURE_SIZE;

        /// <summary>
        /// The height of the GlyphCache.
        /// </summary>
        public static int Height = Constants.DEFAULT_HIDEF_TEXTURE_SIZE;

        /// <summary>
        /// Whether the glyph is generated minimal.
        /// </summary>
        private readonly bool _minimal;

        /// <summary>
        /// The font the GlyphCache is associated with.
        /// </summary>
        private readonly Font _font;

        /// <summary>
        /// The alt font the GlyphCache is associated with.
        /// </summary>
        private readonly Font _altFont;

        /// <summary>
        /// The characters that are part of this GlyphCache.
        /// </summary>
        private readonly List<char> _characters = new List<char>();

        /// <summary>
        /// The current x position in the GlyphCache.
        /// </summary>
        private int _currentX;

        /// <summary>
        /// The current y position in the GlyphCache.
        /// </summary>
        private int _currentY;

        /// <summary>
        /// The manager
        /// </summary>
        private FontManager _manager;

        /// <summary>
        /// Whether the Cache is full (true) or not (false).
        /// </summary>
        public bool Full;

        /// <summary>
        /// The texture the characters are cached on.
        /// </summary>
        public Texture2D Texture;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphCache"/> class.
        /// </summary>
        /// <param name="font">The font.</param>
        /// <param name="manager">The font manager.</param>
        public GlyphCache(Font font, FontManager manager, bool minTextureSize = false, Font altFont = null) {
            _manager = manager;
            _font = font;
            _minimal = minTextureSize;
            _altFont = altFont;
            var surfaceFormat = Constants.DEFAULT_CACHE_SURFACE_FORMAT;
            
            /* Since TML only supports HiDef, this part of code is useless
            switch (_manager.GraphicsDevice.GraphicsProfile) {
                case GraphicsProfile.HiDef:
                    Height = Constants.DEFAULT_HIDEF_TEXTURE_SIZE;
                    Width = Constants.DEFAULT_HIDEF_TEXTURE_SIZE;
                    break;
                case GraphicsProfile.Reach:
                    Height = Constants.DEFAULT_REACH_TEXTURE_SIZE;
                    Width = Constants.DEFAULT_HIDEF_TEXTURE_SIZE;
                    break;
            }
            */
            Height = Constants.DEFAULT_REACH_TEXTURE_SIZE;
            Width = Constants.DEFAULT_HIDEF_TEXTURE_SIZE;

            if (minTextureSize) {
                Width = Constants.DEFAULT_MINIM_TEXTURE_SIZE;
                Height = Constants.DEFAULT_MINIM_TEXTURE_SIZE;
                surfaceFormat = Constants.MINIMAL_CACHE_SURFACE_FORMAT;
            }

            Texture = new Texture2D(_manager.GraphicsDevice, Width, Height, false, surfaceFormat);
        }

        /// <summary>
        /// Adds the character to cache.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="glyph">The cached glyph.</param>
        /// <param name="usedFont">The font that is actual used.</param>
        /// <returns>Whether we could add the character to the cache or not.</returns>
        public bool AddCharacterToCache(char character, out Glyph glyph) {
            var font = _font;
            var index = font.Face.GetCharIndex(character);
            if (index == 0 && _altFont is not null) {
                // Character not found, use alt font
                font = _altFont;
                index = font.Face.GetCharIndex(character);
            }

            font.Face.LoadGlyph(index, font.LoadFlags, font.LoadTarget);
            using var faceGlyph = font.Face.Glyph.GetGlyph();
            faceGlyph.ToBitmap(font.RenderMode, Constants.GlyphBitmapOrigin, true);
            using var bitmap = faceGlyph.ToBitmapGlyph();

            if (_currentX + faceGlyph.Advance.X.Ceiling() >= Width) {
                _currentY += font.GlyphHeight + font.Face.Size.Metrics.NominalHeight;
                _currentX = 0;
            }

            if (_currentY >= Height - font.GlyphHeight) {
                Full = true;
                glyph = null;
                return false;
            }

            glyph = AddGlyph(character, faceGlyph, bitmap, font);
            glyph.Font = font;

            return true;
        }

        /// <summary>
        /// Adds the character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="glyph">The glyph.</param>
        /// <param name="bitmapGlyph">The bitmap glyph.</param>
        /// <returns>The character that we added to the cache.</returns>
        private Glyph AddGlyph(char character, SharpFont.Glyph glyph, BitmapGlyph bitmapGlyph, Font font = null) {
            font ??= _font;
            if (!(bitmapGlyph.Bitmap.Width == 0 || bitmapGlyph.Bitmap.Rows == 0)) {
                var cBox = glyph.GetCBox(GlyphBBoxMode.Pixels);
                var bearingY = (int) font.Face.Size.Metrics.NominalHeight;
                var rectangle = new Rectangle(_currentX + cBox.Left, _currentY + (bearingY - cBox.Top),
                    bitmapGlyph.Bitmap.Width, bitmapGlyph.Bitmap.Rows);
                var dataLength = bitmapGlyph.Bitmap.BufferData.Length;

                if (character < 255 && character != '_') {
                    rectangle.Y += 1;
                }

                if (rectangle.X < 0) {
                    rectangle.Offset(-rectangle.X, 0);
                }

                if (rectangle.Y < 0) {
                    rectangle.Offset(0, -rectangle.Y);
                }

                // if (glyph.Advance.X.Ceiling() != rectangle.Width)
                // {
                //     rectangle.Offset(Math.Abs(rectangle.Width - glyph.Advance.X.Ceiling()) / 2, 0);
                // }

                if (_minimal) {
                    var buffer = new byte[dataLength];
                    for (var i = 0; i < buffer.Length; i++) {
                        var c = bitmapGlyph.Bitmap.BufferData[i] >> 0;
                        buffer[i] = (byte) c;
                    }

                    Texture.SetData(0, rectangle, buffer, 0, dataLength);
                }
                else {
                    var buffer = new ushort[dataLength];
                    for (var i = 0; i < buffer.Length; i++) {
                        var c = bitmapGlyph.Bitmap.BufferData[i] >> 4;
                        buffer[i] = (ushort) (c | (c << 4) | (c << 8) | (c << 12));
                    }

                    Texture.SetData(0, rectangle, buffer, 0, dataLength);
                }
            }

            _characters.Add(character);

            var advanceX = glyph.Advance.X.Ceiling();
            if (character == '\t') {
                advanceX = Math.Abs(font.Face.Size.Metrics.NominalWidth * font.SpacesInTab);
            }

            var finalCharacter = new Glyph(glyph.Advance.X.Ceiling(), font.Face.Size.Metrics.NominalHeight,
                font.Face.Glyph.Metrics.HorizontalBearingX.Ceiling(), font.Face.Size.Metrics.Descender.Ceiling(),
                new Rectangle(_currentX, _currentY, advanceX,
                    font.GlyphHeight + font.Face.Size.Metrics.NominalHeight), character, _characters.Count - 1, this);

            _currentX += advanceX + font.Face.Size.Metrics.NominalWidth;
            return finalCharacter;
        }
    }
}