using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Documents.TextFormatting
{
	/// <summary>
	/// Represents a span of a segment that is rendered on a render line.
	/// </summary>
	internal readonly record struct RenderSegmentSpan(Segment Segment, int GlyphsStart, int GlyphsLength, float Width, float WidthWithoutTrailingSpaces);
}
