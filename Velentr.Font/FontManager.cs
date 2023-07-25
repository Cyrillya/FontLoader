using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FontLoader.Core;
using Microsoft.Xna.Framework.Graphics;
using SharpFont;
using Velentr.Collections.Collections.Concurrent;
using Velentr.Font.Internal;

namespace Velentr.Font
{

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class FontManager : IDisposable
    {

        /// <summary>
        /// The libraries
        /// </summary>
        private ConcurrentPool<Library> _libraries;

        /// <summary>
        /// The fonts we have cached.
        /// </summary>
        public readonly Dictionary<string, Typeface> Typefaces;

        /// <summary>
        /// The minimal fonts we have cached.
        /// </summary>
        public readonly Dictionary<string, Typeface> MinimalTypefaces;

        /// <summary>
        /// The font collections we have cached.
        /// </summary>
        private readonly List<FontCollection> fontCollections;

        /// <summary>
        /// Initializes the <see cref="FontManager"/> class.
        /// </summary>
        public FontManager(GraphicsDevice graphicsDevice)
        {
            _libraries = new ConcurrentPool<Library>(capacity: 1);
            Typefaces = new Dictionary<string, Typeface>();
            MinimalTypefaces = new Dictionary<string, Typeface>();
            fontCollections = new List<FontCollection>();
            GraphicsDevice = graphicsDevice;
        }

        /// <summary>
        /// Gets the library.
        /// </summary>
        /// <returns></returns>
        internal Library GetLibrary()
        {
            return _libraries.Get();
        }

        /// <summary>
        /// Returns the library.
        /// </summary>
        /// <param name="library">The library.</param>
        internal void ReturnLibrary(Library library)
        {
            _libraries.Return(library);
        }

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>
        /// The graphics device.
        /// </value>
        internal GraphicsDevice GraphicsDevice { get; private set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var font in Typefaces.Values)
            {
                font.DisposeFinal();
            }

            while (Loader.InstalledFontLoading) {
                Thread.Sleep(10);
            }

            foreach (var font in MinimalTypefaces.Values)
            {
                font.DisposeFinal();
            }

            foreach (var fontCollection in fontCollections) {
                fontCollection.Dispose();
            }
            
            _libraries.Dispose();
            
            GC.SuppressFinalize(this);
        }

        public void AddFontCollection(FontCollection collection) {
            fontCollections.Add(collection);
        }
        
        /// <summary>
        /// Gets the minimal font.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        public Font GetMinimalFont(string path)
        {
            if (!MinimalTypefaces.TryGetValue(path, out var typeface))
            {
                typeface = new TypefaceImplementation(path, File.ReadAllBytes(path), false, null, false, this);

                MinimalTypefaces.Add(path, typeface);
            }

            var font = typeface.GetFont(16);
            font.MinimalTextureSize = true;
            return font;
        }

        /// <summary>
        /// Gets the font.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="size">The size.</param>
        /// <param name="preGenerateCharacters">if set to <c>true</c> [pre generate characters].</param>
        /// <param name="charactersToPregenerate">The characters to pregenerate.</param>
        /// <param name="storeTypefaceFileData">The store typeface file data.</param>
        /// <returns></returns>
        public Font GetFont(string path, int size, bool preGenerateCharacters = false, char[] charactersToPregenerate = null, bool? storeTypefaceFileData = null)
        {
            var typeface = GetTypefaceInternal(path, File.ReadAllBytes(path), preGenerateCharacters, charactersToPregenerate, storeTypefaceFileData);
            return typeface.GetFont(size, preGenerateCharacters, charactersToPregenerate);
        }

        /// <summary>
        /// Gets the font.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="size">The size.</param>
        /// <param name="preGenerateCharacters">if set to <c>true</c> [pre generate characters].</param>
        /// <param name="charactersToPregenerate">The characters to pregenerate.</param>
        /// <returns></returns>
        public Font GetFont(string name, Stream fileStream, int size, bool preGenerateCharacters = false, char[] charactersToPregenerate = null)
        {
            var buffer = Helpers.ReadStream(fileStream);
            var typeface = GetTypefaceInternal(name, buffer, preGenerateCharacters, charactersToPregenerate, true);
            return typeface.GetFont(size, preGenerateCharacters, charactersToPregenerate);
        }

        /// <summary>
        /// Gets the font.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="fileData">The file data.</param>
        /// <param name="size">The size.</param>
        /// <param name="preGenerateCharacters">if set to <c>true</c> [pre generate characters].</param>
        /// <param name="charactersToPregenerate">The characters to pregenerate.</param>
        /// <returns></returns>
        public Font GetFont(string name, byte[] fileData, int size, bool preGenerateCharacters = false, char[] charactersToPregenerate = null)
        {
            var typeface = GetTypefaceInternal(name, fileData, preGenerateCharacters, charactersToPregenerate, true);
            return typeface.GetFont(size, preGenerateCharacters, charactersToPregenerate);
        }

        /// <summary>
        /// Gets or loads the specified font with the specified size.
        /// </summary>
        /// <param name="path">The path to load the font from.</param>
        /// <param name="preGenerateCharacters">if set to <c>true</c> [pre generate characters].</param>
        /// <param name="charactersToPregenerate">The characters to pregenerate.</param>
        /// <param name="storeTypefaceFileData">Whether to store typeface file data.</param>
        /// <returns>The Font that matches the specified parameters.</returns>
        /// <exception cref="Exception">GraphicsDevice is not initialized! Please either initialize VelentrFont.Core or provide the GraphicsDevice when getting a new font.</exception>
        public Typeface GetTypeface(string path, bool preGenerateCharacters = false, char[] charactersToPregenerate = null, bool? storeTypefaceFileData = null)
        {
            return GetTypefaceInternal(path, File.ReadAllBytes(path), preGenerateCharacters, charactersToPregenerate, storeTypefaceFileData);
        }

        /// <summary>
        /// Gets or loads the specified font with the specified size.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="preGenerateCharacters">if set to <c>true</c> [pre generate characters].</param>
        /// <param name="charactersToPregenerate">The characters to pregenerate.</param>
        /// <returns>The Font that matches the specified parameters.</returns>
        public Typeface GetTypeface(string name, Stream fileStream, bool preGenerateCharacters = false, char[] charactersToPregenerate = null)
        {
            var buffer = Helpers.ReadStream(fileStream);
            return GetTypefaceInternal(name, buffer, preGenerateCharacters, charactersToPregenerate, true);
        }

        /// <summary>
        /// Gets or loads the specified font with the specified size.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="fileData">The file data.</param>
        /// <param name="preGenerateCharacters">if set to <c>true</c> [pre generate characters].</param>
        /// <param name="charactersToPregenerate">The characters to pregenerate.</param>
        /// <returns>The Font that matches the specified parameters.</returns>
        public Typeface GetTypeface(string name, byte[] fileData, bool preGenerateCharacters = false, char[] charactersToPregenerate = null)
        {
            return GetTypefaceInternal(name, fileData, preGenerateCharacters, charactersToPregenerate, true);
        }

        /// <summary>
        /// Gets the typeface internal.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="fileData">The file data.</param>
        /// <param name="preGenerateCharacters">if set to <c>true</c> [pre generate characters].</param>
        /// <param name="charactersToPregenerate">The characters to pregenerate.</param>
        /// <param name="storeTypefaceFileData">The store typeface file data.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">GraphicsDevice is not initialized! Please either initialize VelentrFont.Core or provide the GraphicsDevice when getting a new font.</exception>
        private Typeface GetTypefaceInternal(string name, byte[] fileData, bool preGenerateCharacters, char[] charactersToPregenerate, bool? storeTypefaceFileData)
        {
            if (storeTypefaceFileData == null)
            {
                storeTypefaceFileData = Constants.Settings.StoreFontFileData;
            }

            if (!Typefaces.TryGetValue(name, out var typeface))
            {
                typeface = new TypefaceImplementation(name, fileData, preGenerateCharacters, charactersToPregenerate, (bool)storeTypefaceFileData, this);

                Typefaces.Add(name, typeface);
            }

            return typeface;
        }

        /// <summary>
        /// Gets the stored typeface.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        internal Typeface GetStoredTypeface(string name)
        {
            return Typefaces[name];
        }

        /// <summary>
        /// Removes a font from the system.
        /// </summary>
        /// <param name="key">The key for the font we want to remove.</param>
        internal void RemoveTypeface(string name, bool dispose = true)
        {
            if (Typefaces.ContainsKey(name))
            {
                if (dispose)
                {
                    Typefaces[name].DisposeFinal();
                }

                Typefaces.Remove(name);
            }
        }
    }
}
