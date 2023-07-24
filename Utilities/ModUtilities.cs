using System;
using System.IO;
using FontLoader.ConfigElements;
using FontLoader.Core;
using Velentr.Font;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace FontLoader.Utilities;

public static class ModUtilities
{
    public static FontCollection GetVelentrFont(this Asset<DynamicSpriteFont> dynamicSpriteFont) =>
        GetVelentrFont(dynamicSpriteFont.Value);
    
    public static FontCollection GetVelentrFont(this DynamicSpriteFont dynamicSpriteFont) {
        if (dynamicSpriteFont == FontAssets.MouseText.Value) {
            return FontStatics.FontMouseText;
        }
        if (dynamicSpriteFont == FontAssets.DeathText.Value) {
            return FontStatics.FontDeathText;
        }
        if (dynamicSpriteFont == FontAssets.ItemStack.Value) {
            return FontStatics.FontItemStack;
        }
        if (dynamicSpriteFont == FontAssets.CombatText[0].Value) {
            return FontStatics.FontCombatText;
        }
        if (dynamicSpriteFont == FontAssets.CombatText[1].Value) {
            return FontStatics.FontCombatCrit;
        }

        return null;
    }

    public static float GetSpreadMultipiler(this DynamicSpriteFont dynamicSpriteFont) {
        if (dynamicSpriteFont == FontAssets.DeathText.Value) {
            return 1.5f;
        }
        if (dynamicSpriteFont == FontAssets.CombatText[1].Value) {
            return 1.1f;
        }

        return 1f;
    }

    public static int GetYOffset(this DynamicSpriteFont dynamicSpriteFont) {
        var config = ModContent.GetInstance<Config>();
        
        if (dynamicSpriteFont == FontAssets.DeathText.Value) {
            return 5 + (int)config.BigFontOffsetY;
        }

        return 2 + (int)config.GeneralFontOffsetY;
    }

    public static bool IsTtfOrOtfFile(string filePath) {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;

        string extension = Path.GetExtension(filePath);

        // Check if the extension is either .ttf or .otf (case-insensitive)
        return extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".otf", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsFileOccupied(string filePath)
    {
        FileStream stream = null;
        try
        {
            stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch
        {
            return true;
        }
        finally
        {
            if (stream != null)
            {
                stream.Close();
            }
        }
    }

    public static void Decompress(Stream inStream, Stream outStream)
    {
        byte[] properties = new byte[5];
        if (inStream.Read(properties, 0, 5) != 5)
            throw (new Exception("input .lzma is too short"));
        SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();
        decoder.SetDecoderProperties(properties);

        long outSize = 0;
        for (int i = 0; i < 8; i++)
        {
            int v = inStream.ReadByte();
            if (v < 0)
                throw (new Exception("Can't Read 1"));
            outSize |= ((long)(byte)v) << (8 * i);
        }

        long compressedSize = inStream.Length - inStream.Position;
        decoder.Code(inStream, outStream, compressedSize, outSize, null);
    }
}
