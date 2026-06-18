// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextFormatter.h (FormatLine), tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// Skia implementation of <see cref="TextFormatter"/> over the Uno ParsedText
/// engine.
///
/// WinUI's FormatLine produces one line at a time and threads an opaque
/// TextLineBreak continuation token between calls. ParsedText instead lays out
/// the whole paragraph in a single pass into a list of RenderLines. This adapter
/// reconciles the two (plan "Strategy B"): the first FormatLine for a paragraph
/// runs ParseText once and caches the result, then each call vends the next
/// RenderLine as a <see cref="SkiaTextLine"/> and mints a
/// <see cref="SkiaTextLineBreak"/> carrying the following line index (null on the
/// last line, which terminates the caller's loop).
/// </summary>
internal sealed class SkiaTextFormatter : TextFormatter
{
	// The formatter is stateless apart from a one-entry parse cache. The layout
	// engine formats one paragraph to completion before moving to the next, so a
	// single cached entry (keyed by source identity and wrapping width) is enough
	// to serve every FormatLine call of a paragraph from one ParseText pass.
	public static SkiaTextFormatter Instance { get; } = new();

	private TextSource? _cachedSource;
	private double _cachedWrappingWidth;
	private ParsedText _cachedParsed;
	private bool _hasCache;

	public override TextLine FormatLine(
		TextSource textSource,
		uint firstCharIndex,
		double wrappingWidth,
		TextParagraphProperties textParagraphProperties,
		TextLineBreak? previousLineBreak,
		TextRunCache? textRunCache)
	{
		var index = (previousLineBreak as SkiaTextLineBreak)?.NextLineIndex ?? 0;

		ParsedText parsed;
		if (_hasCache && ReferenceEquals(_cachedSource, textSource) && _cachedWrappingWidth == wrappingWidth)
		{
			parsed = _cachedParsed;
		}
		else
		{
			var source = (ISkiaParagraphSource)textSource;
			parsed = ParsedText.ParseText(
				new Size(wrappingWidth, double.PositiveInfinity),
				source.GetLeafInlines(),
				source.DefaultLineHeight,
				maxLines: 0, // Format every line; paging / MaxLines is applied by the layout tree.
				source.LineHeight,
				source.LineStackingStrategy,
				textParagraphProperties.TextAlignment,
				textParagraphProperties.TextWrapping,
				textParagraphProperties.FlowDirection,
				out _);

			_cachedSource = textSource;
			_cachedWrappingWidth = wrappingWidth;
			_cachedParsed = parsed;
			_hasCache = true;
		}

		var lines = parsed.RenderLines;
		var renderLine = lines[index];
		var nextBreak = index + 1 < lines.Count ? new SkiaTextLineBreak(index + 1) : null;

		return new SkiaTextLine(parsed, renderLine, index, nextBreak, textParagraphProperties);
	}
}
