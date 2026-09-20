// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// The continuation token threaded between <see cref="TextFormatter.FormatLine"/>
/// calls. WinUI uses it to carry opaque line-break state; the Skia formatter lays
/// out the whole paragraph up front, so the token carries that layout and the
/// index of the next line to vend.
/// </summary>
internal sealed class SkiaTextLineBreak : TextLineBreak
{
	public SkiaTextLineBreak(TextSource textSource, ParsedText parsedText, double wrappingWidth, int nextLineIndex)
	{
		TextSource = textSource;
		ParsedText = parsedText;
		WrappingWidth = wrappingWidth;
		NextLineIndex = nextLineIndex;
	}

	public TextSource TextSource { get; }

	public ParsedText ParsedText { get; }

	public double WrappingWidth { get; }

	public int NextLineIndex { get; }
}
