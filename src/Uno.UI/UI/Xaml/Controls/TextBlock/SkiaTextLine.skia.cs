#nullable enable

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Composition
{
	/// <summary>
	/// Because Skia doesn't support fallback fonts for specific parts of text, we manually handle that in this class.
	/// </summary>
	internal class SkiaTextLine
	{
		private readonly List<(string Text, SKTypeface? Font)> _parts = new List<(string Text, SKTypeface? Font)>();
		public IReadOnlyCollection<(string Text, SKTypeface? Font)> TextParts => _parts.AsReadOnly();

		public SkiaTextLine(string text, SKTypeface? font, Func<string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant, SKTypeface> getTypeFace)
		{
			if (text is null)
			{
				throw new ArgumentNullException(nameof(text));
			}

			if (text.Length == 0)
			{
				_parts.Add((text, font));
				return;
			}

			var builder = new StringBuilder(capacity: text.Length);
			builder.Append(text[0]);
			SKTypeface? lastValidFont = GetFontForCharacter(text[0], font, getTypeFace);
			for (var i = 1; i < text.Length; i++)
			{
				var character = text[i];
				var currentFont = GetFontForCharacter(character, font, getTypeFace);
				if (lastValidFont != currentFont)
				{
					_parts.Add((builder.ToString(), lastValidFont));
					builder.Clear();
					lastValidFont = currentFont;
				}

				builder.Append(character);
			}

			if (builder.Length > 0)
			{
				_parts.Add((builder.ToString(), lastValidFont));
			}
		}

		private static SKTypeface? GetFontForCharacter(char character, SKTypeface? font, Func<string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant, SKTypeface> getTypeFace)
		{
			if (font?.ContainsGlyph(character) ?? true)
			{
				return font;
			}

			var fontForCharacter = SKFontManager.Default.MatchCharacter(character);
			if (fontForCharacter is null)
			{
				return font;
			}

			return getTypeFace(fontForCharacter.FamilyName, (SKFontStyleWeight)font.FontWeight, (SKFontStyleWidth)font.FontStyle.Width, font.FontSlant);
		}
	}
}
