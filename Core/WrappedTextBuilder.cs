using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FontLoader.Utilities;
using ReLogic.Text;
using Velentr.Font;

namespace FontLoader.Core;

public class WrappedTextBuilder
{
	private struct NonBreakingText
	{
		public readonly string Text;
		public readonly float Width;
		public readonly bool IsWhitespace;
		private FontCollection _font;

		public NonBreakingText(FontCollection font, string text)
		{
			Text = text;
			_font = font;
			Width = font.MeasureText(text).X;
            IsWhitespace = text.All(c => c == ' ');
        }

		public string GetAsWrappedText(float maxWidth)
		{
			StringBuilder stringBuilder = new StringBuilder(Text.Length);
			StringBuilder curLineBuilder = new StringBuilder(Text.Length);
			for (int i = 0; i < Text.Length; i++) {
				float width = _font.MeasureText(curLineBuilder.ToString() + Text[i]).X;
				if (width > maxWidth) {
					curLineBuilder = new StringBuilder(Text.Length);
					stringBuilder.Append('\n');
				}

				stringBuilder.Append(Text[i]);
				curLineBuilder.Append(Text[i]);
			}

			return stringBuilder.ToString();
		}
	}

	private readonly FontCollection _font;
	private readonly CultureInfo _culture;
	private readonly float _maxWidth;
	private readonly StringBuilder _completedText = new();
	private readonly StringBuilder _workingLine = new();

	public WrappedTextBuilder(FontCollection font, float maxWidth, CultureInfo culture)
	{
		_font = font;
		_maxWidth = maxWidth;
		_culture = culture;
	}

	public void CommitWorkingLine()
	{
		if (!_completedText.IsEmpty())
			_completedText.Append('\n');

		_completedText.Append(_workingLine);
		_workingLine.Clear();
	}

	private void Append(NonBreakingText textToken)
	{
		float num = !_workingLine.IsEmpty() ? _font.MeasureText(_workingLine + textToken.Text).X : textToken.Width;
		if (textToken.Width > _maxWidth) {
			if (!_workingLine.IsEmpty())
				CommitWorkingLine();

			if (textToken.Text.Length == 1) {
				_workingLine.Append(textToken.Text);
			}
			else {
				Append(textToken.GetAsWrappedText(_maxWidth));
			}
		}
		else if (num <= _maxWidth) {
			_workingLine.Append(textToken.Text);
		}
		else if (_workingLine.IsEmpty()) {
			_completedText.Append(textToken.Text);
			_workingLine.Clear();
		}
		else {
			CommitWorkingLine();
			if (!textToken.IsWhitespace) {
				_workingLine.Append(textToken.Text);
			}
		}
	}

	public void Append(string text)
	{
		StringReader stringReader = new StringReader(text);
		_completedText.EnsureCapacity(_completedText.Capacity + text.Length);
		while (stringReader.Peek() > 0) {
			if (stringReader.Peek() == '\n') {
				stringReader.Read();
				CommitWorkingLine();
			}
			else {
				string text2 = stringReader.ReadUntilBreakable(_culture);
				Append(new NonBreakingText(_font, text2));
			}
		}
	}

	public override string ToString()
	{
		if (_completedText.IsEmpty())
			return _workingLine.ToString();

		return _completedText.ToString() + "\n" + _workingLine;
	}
}
