using System;
using System.Collections.Generic;
using System.IO;

namespace FontLoader.Utilities;

public static class DylibArchitectureChecker
{
    private const uint MH_MAGIC = 0xFEEDFACE;
    private const uint MH_CIGAM = 0xCEFAEDFE;
    private const uint MH_MAGIC_64 = 0xFEEDFACF;
    private const uint MH_CIGAM_64 = 0xCFFAEDFE;
    private const uint FAT_MAGIC = 0xCAFEBABE;
    private const uint FAT_CIGAM = 0xBEBAFECA;
    private const uint FAT_MAGIC_64 = 0xCAFEBABF;
    private const uint FAT_CIGAM_64 = 0xBFBABECA;

    private const int CPU_TYPE_X86_64 = 0x01000007;
    private const int CPU_TYPE_ARM64 = 0x0100000C;

    public static bool HasExpectedArch(string filePath, bool expectArm64, out string archSummary) {
        archSummary = "unreadable";

        try {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(fs);

            uint magic = br.ReadUInt32();

            bool thinLittleEndian = magic == MH_MAGIC || magic == MH_MAGIC_64;
            bool thinBigEndian = magic == MH_CIGAM || magic == MH_CIGAM_64;
            bool fatLittleEndian = magic == FAT_MAGIC || magic == FAT_MAGIC_64;
            bool fatBigEndian = magic == FAT_CIGAM || magic == FAT_CIGAM_64;

            var archs = new HashSet<string>();

            if (thinLittleEndian || thinBigEndian) {
                bool littleEndian = thinLittleEndian;
                int cpuType = ReadInt32(br, littleEndian);
                AddArchName(archs, cpuType);
            }
            else if (fatLittleEndian || fatBigEndian) {
                bool littleEndian = fatLittleEndian;
                bool isFat64 = magic == FAT_MAGIC_64 || magic == FAT_CIGAM_64;

                uint count = ReadUInt32(br, littleEndian);
                for (uint i = 0; i < count; i++) {
                    int cpuType = ReadInt32(br, littleEndian);
                    AddArchName(archs, cpuType);

                    int bytesToSkip = isFat64 ? 28 : 16;
                    br.BaseStream.Seek(bytesToSkip, SeekOrigin.Current);
                }
            }
            else {
                archSummary = "unknown-magic";
                return false;
            }

            archSummary = archs.Count == 0 ? "none" : string.Join(",", archs);
            return expectArm64 ? archs.Contains("arm64") : archs.Contains("x86_64");
        }
        catch {
            return false;
        }
    }

    private static uint ReadUInt32(BinaryReader br, bool littleEndian) {
        uint value = br.ReadUInt32();
        return littleEndian ? value : Swap32(value);
    }

    private static int ReadInt32(BinaryReader br, bool littleEndian) {
        return unchecked((int) ReadUInt32(br, littleEndian));
    }

    private static uint Swap32(uint value) {
        return (value >> 24)
               | ((value >> 8) & 0x0000FF00)
               | ((value << 8) & 0x00FF0000)
               | (value << 24);
    }

    private static void AddArchName(HashSet<string> archs, int cpuType) {
        if (cpuType == CPU_TYPE_ARM64) {
            archs.Add("arm64");
            return;
        }

        if (cpuType == CPU_TYPE_X86_64) {
            archs.Add("x86_64");
            return;
        }

        archs.Add($"cpu:{cpuType}");
    }
}