// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextFormatter.h (FormatLine), tag winui3/release/2.4.0, commit e8442d07a

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
/// reconciles the two (plan "Strategy B"): a FormatLine without a previous break
/// runs ParseText from the current inputs, then each call vends the next
/// RenderLine as a <see cref="SkiaTextLine"/> and mints a
/// <see cref="SkiaTextLineBreak"/> carrying that layout and the following line
/// index (null on the last line, which terminates the caller's loop).
/// </summary>
internal sealed class SkiaTextFormatter : TextFormatter
{
	// Stateless: like Line Services, formatted lines never outlive the break chain that produced them.
	public static SkiaTextFormatter Instance { get; } = new();

	public override TextLine FormatLine(
		TextSource textSource,
		uint firstCharIndex,
		double wrappingWidth,
		TextParagraphProperties textParagraphProperties,
		TextLineBreak? previousLineBreak,
		TextRunCache? textRunCache)
	{
		var previousBreak = previousLineBreak as SkiaTextLineBreak;

		ParsedText parsed;
		int index;
		if (previousBreak is not null && ReferenceEquals(previousBreak.TextSource, textSource) && previousBreak.WrappingWidth == wrappingWidth)
		{
			// A continuation resumes its own pass.
			parsed = previousBreak.ParsedText;
			index = previousBreak.NextLineIndex;
		}
		else
		{
			// Another source (an overflow's paragraph, which also hosts its inline objects) or width re-formats.
			// Line ordinals don't carry across layouts, so resume at the character break as LsCreateLine does.
			parsed = Parse((ISkiaParagraphSource)textSource, wrappingWidth, textParagraphProperties, (int)firstCharIndex, out index);
		}

		var lines = parsed.RenderLines;
		var renderLine = lines[index];
		var nextBreak = index + 1 < lines.Count ? new SkiaTextLineBreak(textSource, parsed, wrappingWidth, index + 1) : null;

		return new SkiaTextLine(parsed, renderLine, index, nextBreak, textParagraphProperties);
	}

	private static ParsedText Parse(ISkiaParagraphSource source, double wrappingWidth, TextParagraphProperties textParagraphProperties, int resumeCharIndex, out int resumeLineIndex)
		=> ParsedText.ParseText(
			new Size(wrappingWidth, double.PositiveInfinity),
			source.GetLeafInlines(),
			source.DefaultLineHeight,
			maxLines: 0, // Format every line; paging / MaxLines is applied by the layout tree.
			source.LineHeight,
			source.LineStackingStrategy,
			textParagraphProperties.TextLineBounds,
			textParagraphProperties.TextAlignment,
			textParagraphProperties.TextWrapping,
			textParagraphProperties.FlowDirection,
			out _,
			resumeCharIndex,
			out resumeLineIndex,
			container => source.FormatInlineObject(container, (float)wrappingWidth));
}
