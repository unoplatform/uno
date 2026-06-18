// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Documents;

partial class Paragraph
{
	// CParagraph::GetInlineCollection — the paragraph's inline content collection.
	internal InlineCollection GetInlineCollection() => Inlines;

	// TODO Uno (Stage 4): Paragraph.GetPositionCount — number of text positions in the paragraph's
	// backing store (CParagraph::GetPositionCount).
	internal void GetPositionCount(out uint count)
		=> throw new NotSupportedException("TODO Uno (Stage 4): Paragraph.GetPositionCount");

	// TODO Uno (Stage 4): Paragraph.GetRun — yields one run's formatting, inherited attached
	// properties, nesting type, nested element and character span (CParagraph::GetRun).
	internal void GetRun(
		uint characterIndex,
		out object? pFormatting,
		out InheritedProperties? pInheritedAttachedProperties,
		out TextNestingType nestingType,
		out TextElement? pNestedElement,
		out ReadOnlyMemory<char> pCharacters,
		out uint cCharacters)
		=> throw new NotSupportedException("TODO Uno (Stage 4): Paragraph.GetRun");
}
