using System;
using System.IO;

namespace FontLoader.Utilities;

public class FontFileTypeChecker
{
    // TTF file signature: 00 01 00 00
    private static readonly byte[] TtfSignature = {0x00, 0x01, 0x00, 0x00};

    // OTF file signature: 4F 54 54 4F
    private static readonly byte[] OtfSignature = {0x4F, 0x54, 0x54, 0x4F};

    // TTC file signature: 74 74 63 66 (ASCII: "ttcf")
    private static readonly byte[] TtcSignature = {0x74, 0x74, 0x63, 0x66};

    public static bool IsFontFile(string filePath) {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;

        string extension = Path.GetExtension(filePath);

        // Check if the extension is either .ttf, .otf, or .ttc (case-insensitive)
        if (!extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".otf", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".ttc", StringComparison.OrdinalIgnoreCase)) return false;

        try {
            // Read the first 5 bytes of the file
            byte[] fileBytes = new byte[4];
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                fileStream.Read(fileBytes, 0, 4);
            }

            // Check if the file matches the TTF, OTF, or TTC signatures
            return CompareByteArrays(fileBytes, TtfSignature) ||
                   CompareByteArrays(fileBytes, OtfSignature) ||
                   CompareByteArrays(fileBytes, TtcSignature);
        }
        catch (IOException ex) {
            Console.WriteLine($"File name: {filePath} \nError reading the file: {ex.Message}");
            return false;
        }
    }

    private static bool CompareByteArrays(byte[] arr1, byte[] arr2) {
        if (arr1.Length != arr2.Length) {
            return false;
        }

        for (int i = 0; i < arr1.Length; i++) {
            if (arr1[i] != arr2[i]) {
                return false;
            }
        }

        return true;
    }
}