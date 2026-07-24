// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextFormatter.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// Provides services for formatting text and breaking text lines.
/// </summary>
internal abstract class TextFormatter
{
	// Creates a TextLine that is used for formatting and displaying text content.
	public abstract TextLine FormatLine(
		TextSource textSource,
		uint firstCharIndex,
		double wrappingWidth,
		TextParagraphProperties textParagraphProperties,
		TextLineBreak? previousLineBreak,
		TextRunCache? textRunCache);

	// TODO Uno: The WinUI static Create(IFontAndScriptServices) factory built
	// the LineServices-backed formatter. On Uno the concrete formatter wraps the
	// Skia ParsedText engine and is created by that bridge (see SkiaTextFormatter).
}
