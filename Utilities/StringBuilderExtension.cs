using System.Text;

namespace FontLoader.Utilities;

internal static class StringBuilderExtension
{
	internal static bool IsEmpty(this StringBuilder stringBuilder) => stringBuilder.Length == 0;
}
