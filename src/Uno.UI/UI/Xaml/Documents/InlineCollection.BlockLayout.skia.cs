// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Documents;

partial class InlineCollection
{
	// TODO Uno (Stage 4): InlineCollection.GetPositionCount — number of text positions in the
	// collection's backing store (CInlineCollection::GetPositionCount).
	internal void GetPositionCount(out uint count)
		=> throw new NotSupportedException("TODO Uno (Stage 4): InlineCollection.GetPositionCount");

	// TODO Uno (Stage 4): InlineCollection.GetRun — yields one run's formatting, inherited attached
	// properties, nesting type, nested element and character span (CInlineCollection::GetRun).
	internal void GetRun(
		uint characterIndex,
		out object? pFormatting,
		out InheritedProperties? pInheritedAttachedProperties,
		out TextNestingType nestingType,
		out TextElement? pNestedElement,
		out ReadOnlyMemory<char> pCharacters,
		out uint cCharacters)
		=> throw new NotSupportedException("TODO Uno (Stage 4): InlineCollection.GetRun");
}
