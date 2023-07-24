using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FontLoader.ConfigElements;

[JsonConverter(typeof(StringEnumConverter))]
public enum OverrideType : byte
{
    MouseText,
    CombatText,
    CombatCrit,
    ItemStack,
    DeathText
}

[JsonConverter(typeof(StringEnumConverter))]
public enum SearchPathMode : byte
{
    SystemAndUser,
    SystemOnly,
    CustomPath
}