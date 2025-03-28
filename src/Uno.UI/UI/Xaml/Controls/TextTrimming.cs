using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml
{
	public enum TextTrimming
	{
		/// <summary>
		/// [NOT SUPPORTED, Fallback = 'Clip']
		/// Text is not trimmed.
		/// </summary>
		None = 0,

		/// <summary>
		/// Text is trimmed at a character boundary. An ellipsis (...) is drawn in place of remaining text.
		/// </summary>
		CharacterEllipsis = 1,

		/// <summary>
		/// [NOT SUPPORTED, Fallback = 'CharacterEllipsis']
		/// Text is trimmed at a word boundary. An ellipsis (...) is drawn in place of remaining text.
		/// </summary>
		WordEllipsis = 2,

		/// <summary>
		/// Text is trimmed at a pixel level, visually clipping the excess glyphs.
		/// </summary>
		Clip = 3,
	}
}
