// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Inline.cpp (CInline), tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents;

partial class Inline
{
	// CInline::GetRun — Implemented in derived classes of Inline (Run/Span/LineBreak/InlineUIContainer).
	internal virtual void GetRun(
		uint characterPosition,
		out RichTextServices.TextFormatting? ppTextFormatting,
		out InheritedProperties? ppInheritedProperties,
		out TextNestingType pNestingType,
		out TextElement? ppNestedElement,
		out ReadOnlyMemory<char> ppCharacters,
		out uint pcCharacters)
	{
		MUX_ASSERT(false);
		throw new InvalidOperationException(); // Implemented in derived classes of Inline
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Base class implementation of GetElementEdgeOffset for flat inlines.
	//      Inlines supporting nesting (Span) need to override to search
	//      their nested collections.
	//------------------------------------------------------------------------
	internal virtual void GetElementEdgeOffset(
		TextElement pElement,
		ElementEdge edge,
		out uint pOffset,
		out bool pFound)
	{
		uint offset = 0;
		bool found = false;

		if (pElement == this)
		{
			found = true;
			GetOffsetForEdge(edge, out offset);
		}

		pOffset = offset;
		pFound = found;
	}
}
