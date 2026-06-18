// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference paragraph.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Documents;

partial class Paragraph
{
	//------------------------------------------------------------------------
	//    Summary:
	//        Retrieves the number of positions in the span.
	//
	//------------------------------------------------------------------------
	internal override void GetPositionCount(out uint pcPositions)
	{
		pcPositions = 0;

		if (Inlines.Count > 0)
		{
			Inlines.GetPositionCount(out pcPositions);
		}
		else
		{
			pcPositions = 2;
		}
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves a text run from a given character position.
	//
	//------------------------------------------------------------------------
	internal override void GetRun(
		uint characterPosition,
		out RichTextServices.TextFormatting? ppTextFormatting,
		out InheritedProperties? ppInheritedProperties,
		out TextNestingType pNestingType,
		out TextElement? ppNestedElement,
		out ReadOnlyMemory<char> ppCharacters,
		out uint pcCharacters)
	{
		if (Inlines.Count > 0)
		{
			Inlines.GetRun(
				characterPosition,
				out ppTextFormatting,
				out ppInheritedProperties,
				out pNestingType,
				out ppNestedElement,
				out ppCharacters,
				out pcCharacters);

			// Inline collection for its boundaries cannot set ppNestedElement, since it does not
			// have information about its parent. Set ppNestedElement, if it wasn't already set.
			if (ppNestedElement == null &&
				(pNestingType == TextNestingType.OpenNesting || pNestingType == TextNestingType.CloseNesting))
			{
#if DEBUG
				Inlines.GetPositionCount(out var length);
				global::System.Diagnostics.Debug.Assert(characterPosition == 0 || characterPosition == length - 1);
#endif
				ppNestedElement = this;
			}
		}
		else
		{
			// Empty Paragraph just has start/end tag positions. This is the same behavior as Span.
			// We could return formatting here, but it is pointless since nested runs will pick up anything inherited.
			if (characterPosition >= 2)
			{
				throw new ArgumentOutOfRangeException(nameof(characterPosition));
			}

			ppCharacters = ReadOnlyMemory<char>.Empty;
			pcCharacters = 1;

			ppTextFormatting = null;
			ppInheritedProperties = null;

			pNestingType = (characterPosition == 0) ? TextNestingType.OpenNesting : TextNestingType.CloseNesting;

			ppNestedElement = this;
		}
	}

	//------------------------------------------------------------------------
	//    Summary:
	//        Retrieves the containing nested text element from a character position.
	//
	//------------------------------------------------------------------------
	internal override void GetContainingElement(
		uint characterPosition,
		out TextElement? ppContainingElement)
	{
		TextElement? pElement = null;

		Inlines.GetContainingElement(characterPosition, out pElement);

		if (pElement == null)
		{
			// If the paragraph either doesn't have inlines or couldn't match a containing element in
			// the inline collection, hit test paragraph itself.
			base.GetContainingElement(characterPosition, out pElement);
		}

		ppContainingElement = pElement;
	}

	internal override void GetElementEdgeOffset(
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
		else
		{
			// Try nested inlines.
			Inlines.GetElementEdgeOffset(pElement, edge, out offset, out found);
		}

		pOffset = offset;
		pFound = found;
	}
}
