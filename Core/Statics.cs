using System.Reflection;
using Velentr.Font;

namespace FontLoader.Core;

public class Statics
{
    internal static FontManager Manager;
    internal static FontCollection FontDeathText;
    internal static FontCollection FontMouseText;
    internal static FontCollection FontCombatText;
    internal static FontCollection FontCombatCrit;
    internal static FontCollection FontItemStack;
    internal static byte[] PingFangBytes;
    internal static FieldInfo LoadModsField;
    internal static MethodInfo SetTextMethod;
}