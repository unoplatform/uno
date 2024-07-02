using System;
using System.Collections.Generic;
using System.Diagnostics;
using SkiaSharp;

#nullable enable

namespace Windows.UI.Xaml.Documents.TextFormatting
{
	/// <summary>
	/// Represents a stand-alone line break or a segment of a Run that can end in a word-break opportunity and/or a line break. All glyphs in a segment go in
	/// the same direction (LTR or RTL).
	/// </summary>
	[DebuggerDisplay("{DebugText}")]
	internal sealed class Segment
	{
		// Measured by hand from WinUI. Oddly enough, it doesn't depend on the font size.
		private const float TabStopWidth = 50;

		private readonly List<GlyphInfo>? _glyphs;
		private readonly FontDetails? _fallbackFont;
		// we cache the text as soon as we create the Segment in case a Run's text Updates
		// before this (now outdated) Segment is discarded.
		private readonly string _text;

		/// <summary>
		/// Gets either the LineBreak element or Run element that this segment was created from.
		/// </summary>
		public Inline Inline { get; }

		/// <summary>
		/// The flow direction of the text.
		/// </summary>
		public FlowDirection Direction { get; }

		/// <summary>
		/// Gets the starting index of the character in the Run element text this segment represents. Returns 0 for segments that represent a LineBreak element.
		/// </summary>
		public int Start { get; }

		/// <summary>
		/// Gets the number of characters in the Run element text this segment represents. Returns 0 for segments that represent a LineBreak element.
		/// </summary>
		public int Length { get; }

		/// <summary>
		/// Gets the number of leading space characters in the Run element text this segment represents. Returns 0 for segments that represent a LineBreak
		/// element.
		/// </summary>
		public int LeadingSpaces { get; }

		/// <summary>
		/// Gets the number of trailing space characters in the Run element text this segment represents, before any line break characters. Returns 0 for
		/// segments that represent a LineBreak element.
		/// </summary>
		public int TrailingSpaces { get; }

		/// <summary>
		/// Gets a value indicating whether a line break should be performed after this segment. Returns true if this segment represents a LineBreak element or
		/// part of a Run that ends in a line break character.
		/// </summary>
		public bool LineBreakAfter { get; }

		/// <summary>
		/// Gets the number of characters that represent the line break at the end of the Run element text this segment represents. Value can be 1 or 2 if there
		/// is a line break at the end of the segment text (i.e. 1 for just "\n" or 2 for "\r\n"), otherwise the value is 0. Returns 0 for segments that
		/// represent a LineBreak element.
		/// </summary>
		public int LineBreakLength { get; }

		/// <summary>
		/// Returns a value indicating whether the end of this segment is a word break opportunity.
		/// </summary>
		public bool WordBreakAfter { get; }

		public bool IsTab => Text.Length == 1 && Text[0] == '\t';

		/// <summary>
		/// Gets the section of text of the Run element this segment represents. Throws if this segment represents a LineBreak element.
		/// </summary>
		public ReadOnlySpan<char> Text => Inline is Run run ? _text.AsSpan().Slice(Start, Length) : throw new InvalidOperationException("Text can only be retrieved for segments representing part of a Run.");

		/// <summary>
		/// Gets the glyphs for the Run element text this segment represents. RTL segments return the glyphs in the order the clusters appear in the text
		/// string, from right to left. Throws if this segment represents a LineBreak element.
		/// </summary>
		public IReadOnlyList<GlyphInfo> Glyphs => _glyphs ?? throw new InvalidOperationException("Glyphs can only be retrieved for segments representing part of a Run.");

		private string DebugText => Inline is Run ? Text.ToString() : "{LineBreak}";

		public Segment(Run run, FlowDirection direction, int start, int length, int leadingSpaceCount, int trailingSpaceCount, int lineBreakLength, bool wordBreakAfter, List<GlyphInfo> glyphs, FontDetails? fallbackFont)
		{
			Inline = run;
			Direction = direction;
			Start = start;
			Length = length;
			LeadingSpaces = leadingSpaceCount;
			TrailingSpaces = trailingSpaceCount;
			LineBreakLength = lineBreakLength;
			LineBreakAfter = lineBreakLength > 0;
			WordBreakAfter = wordBreakAfter;
			_glyphs = glyphs;
			_fallbackFont = fallbackFont;
			_text = run.Text;
		}

		public Segment(LineBreak lineBreak)
		{
			Inline = lineBreak;
			LineBreakAfter = true;
			_text = String.Empty;
		}

		public FontDetails? FallbackFont => _fallbackFont;

		/// <remarks>
		/// This is the only form of impurity in this class. Unfortunately, we can't
		/// calculate the width of the tab ahead of time.
		/// </remarks>
		public void AdjustTabWidth(float xOffset)
		{
			Debug.Assert(IsTab);
			_glyphs![0] = _glyphs[0] with { AdvanceX = TabStopWidth - xOffset % TabStopWidth };
		}
	}
}
