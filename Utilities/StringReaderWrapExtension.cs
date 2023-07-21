using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FontLoader.Utilities;

internal static class StringReaderWrapExtension
{
	internal enum WrapScanMode
	{
		Space,
		NewLine,
		Word,
		None
	}

	private static readonly HashSet<char> InvalidCharactersForLineStart = new HashSet<char> {
		'!',
		'%',
		')',
		',',
		'.',
		':',
		';',
		'?',
		']',
		'}',
		'¢',
		'°',
		'·',
		'’',
		'"',
		'"',
		'†',
		'‡',
		'›',
		'℃',
		'∶',
		'、',
		'。',
		'〃',
		'〆',
		'〕',
		'〗',
		'〞',
		'﹚',
		'﹜',
		'！',
		'＂',
		'％',
		'＇',
		'）',
		'，',
		'．',
		'：',
		'；',
		'？',
		'！',
		'］',
		'｝',
		'～',
		' ',
		'\n'
	};
	private static readonly HashSet<char> InvalidCharactersForLineEnd = new HashSet<char> {
		'$',
		'(',
		'£',
		'¥',
		'·',
		'‘',
		'"',
		'〈',
		'《',
		'「',
		'『',
		'【',
		'〔',
		'〖',
		'〝',
		'﹙',
		'﹛',
		'＄',
		'（',
		'．',
		'［',
		'｛',
		'￡',
		'￥'
	};
	private static readonly CultureInfo[] SupportedCultures = new CultureInfo[9] {
		new CultureInfo("en-US"),
		new CultureInfo("de-DE"),
		new CultureInfo("it-IT"),
		new CultureInfo("fr-FR"),
		new CultureInfo("es-ES"),
		new CultureInfo("ru-RU"),
		new CultureInfo("zh-Hans"),
		new CultureInfo("pt-BR"),
		new CultureInfo("pl-PL")
	};
	private static readonly CultureInfo SimplifiedChinese = new CultureInfo("zh-Hans");

	internal static bool IsCultureSupported(CultureInfo culture) => SupportedCultures.Contains(culture);

	internal static bool IsIgnoredCharacter(char character)
	{
		if (character < ' ')
			return character != '\n';

		return false;
	}

	internal static bool CanBreakBetween(char previousChar, char nextChar, CultureInfo culture)
	{
		if (culture.LCID == SimplifiedChinese.LCID) {
			if (!InvalidCharactersForLineEnd.Contains(previousChar))
				return !InvalidCharactersForLineStart.Contains(nextChar);

			return false;
		}

		return false;
	}

	internal static WrapScanMode GetModeForCharacter(char character)
	{
		if (IsIgnoredCharacter(character))
			return WrapScanMode.None;

		switch (character) {
			case '\n':
				return WrapScanMode.NewLine;
			case ' ':
				return WrapScanMode.Space;
			default:
				return WrapScanMode.Word;
		}
	}

	internal static string ReadUntilBreakable(this StringReader reader, CultureInfo culture)
	{
		StringBuilder stringBuilder = new StringBuilder();
		char c = (char)reader.Peek();
		WrapScanMode wrapScanMode = WrapScanMode.None;
		while (reader.Peek() > 0) {
			if (IsIgnoredCharacter((char)reader.Peek())) {
				reader.Read();
				continue;
			}

			char previousChar = c;
			c = (char)reader.Peek();
			WrapScanMode wrapScanMode2 = wrapScanMode;
			wrapScanMode = GetModeForCharacter(c);
			if (!stringBuilder.IsEmpty() && wrapScanMode2 != wrapScanMode)
				return stringBuilder.ToString();

			if (stringBuilder.IsEmpty()) {
				stringBuilder.Append((char)reader.Read());
				continue;
			}

			if (CanBreakBetween(previousChar, c, culture))
				return stringBuilder.ToString();

			stringBuilder.Append((char)reader.Read());
		}

		return stringBuilder.ToString();
	}
}
