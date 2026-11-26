#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// The drawing context ParagraphNode draws its lines into. WinUI's records glyph runs; here a line is drawn whole by
/// <see cref="UnicodeText"/>, which also owns highlights, decorations and backplates, so the context records which line
/// goes where and the page painter draws them.
/// </summary>
internal sealed class RecordedTextDrawingContext : TextDrawingContext
{
	/// <param name="Origin">The line box's top-left, in the paragraph's flow space.</param>
	/// <param name="BandTop">The top of the line's advance, which highlights fill.</param>
	/// <param name="BandHeight">The line's vertical advance.</param>
	internal readonly record struct LineRecord(UnicodeTextLine Line, Point Origin, double BandTop, double BandHeight);

	private readonly List<LineRecord> _lines = new();
	private double _bandTop;
	private double _bandHeight;

	internal IReadOnlyList<LineRecord> Lines => _lines;

	internal void Record(UnicodeTextLine line, Point origin) => _lines.Add(new LineRecord(line, origin, _bandTop, _bandHeight));

	public override void SetLineInfo(double viewportWidth, bool invertHorizontalAxis, double yOffset, double verticalAdvance)
	{
		_bandTop = yOffset;
		_bandHeight = verticalAdvance;
	}

	public override void Clear() => _lines.Clear();

	public override bool HasRenderingData => _lines.Count > 0;

	public override void DrawGlyphRun(Point position, double runWidth, IReadOnlyList<GlyphInfo> glyphs, FontDetails font, WeakReference<DependencyObject>? brushSource, Rect? clipRect)
		=> throw new NotSupportedException("Lines are recorded whole.");

	public override void DrawLine(Point position, double width, double thickness, byte bidirectionalLevel, WeakReference<DependencyObject>? brushSource, Rect? clipRect)
		=> throw new NotSupportedException("Lines are recorded whole.");

	public override void SetIsColorFontEnabled(bool isColorFontEnabled)
	{
	}

	public override void SetColorFontPaletteIndex(uint colorFontPaletteIndex)
	{
	}
}
