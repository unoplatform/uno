// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// Uno-specific bridge between a <see cref="TextSource"/> and the Skia
/// <see cref="ParsedText"/> engine. WinUI feeds the line formatter character
/// runs one at a time; Uno's ParsedText lays out a whole paragraph in a single
/// pass from its leaf inlines, so the Uno text source exposes that input here.
/// </summary>
internal interface ISkiaParagraphSource
{
	// The pre-order leaf inlines of the paragraph, as consumed by ParsedText.
	Inline[] GetLeafInlines();

	// The default line height (the default font's size), used when the paragraph
	// is empty.
	float DefaultLineHeight { get; }

	// The resolved line height for the paragraph (0 = automatic).
	float LineHeight { get; }

	// The resolved line stacking strategy for the paragraph.
	LineStackingStrategy LineStackingStrategy { get; }

	// Creates and formats an ObjectRun for every InlineUIContainer in the paragraph, measuring its
	// child against the embedded element host. WinUI does this per line from GetTextRun; object runs
	// have a fixed size, so ParsedText can measure them once per paragraph instead.
	// Returns null when the paragraph has no InlineUIContainer.
	IReadOnlyDictionary<InlineUIContainer, (ObjectRun Run, ObjectRunMetrics Metrics)>? FormatInlineObjects(float paragraphWidth);
}
