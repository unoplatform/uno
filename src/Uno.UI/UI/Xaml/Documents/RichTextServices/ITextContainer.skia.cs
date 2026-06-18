// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextContainer.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using Microsoft.UI.Xaml.Documents;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

//------------------------------------------------------------------------
//
//  Interface:  ITextContainer
//
//  Synopsis:
//      APIs that provide access to character content
//
//------------------------------------------------------------------------
internal interface ITextContainer
{
	//------------------------------------------------------------------------
	//
	//  Method:   ITextContainer::GetCore
	//
	//  Synopsis: Returns the CoreServices object.
	//
	//------------------------------------------------------------------------
	Uno.UI.Xaml.Core.CoreServices GetCore();

	DependencyObject GetAsDependencyObject();

	//------------------------------------------------------------------------
	//
	//  Method:   ITextContainer::GetPositionCount
	//
	//  Synopsis: Returns the number of character positions covered by the
	//            text content.
	//
	//  Each UTF-16 code unit takes a character position. Additionally, every
	//  Inline or InlineCollection reserves 2 positions corresponding
	//  to its start and end.
	//
	//------------------------------------------------------------------------
	void GetPositionCount(out uint pcPositions);

	//------------------------------------------------------------------------
	//
	//  Method:   ITextContainer::GetRun
	//
	//  Synopsis: Returns a run of characters all of the same format starting
	//            at the given position.
	//
	//  If the startPosition corresponds to the (reserved) start or end
	//  position of an Inline, GetRun returns
	//    1) *ppCharacters == NULL,
	//    2) *pTextFormatting == NULL,
	//    3) *pcCharacters == number of reserved positions (1 for start or end of Inline).
	//
	//  If the startPosition corresponds to a (UTF-16) code unit, GetRun
	//  returns
	//    1) *ppCharacters points to that code unit,
	//    2) *pcCharacters is set to the length of the longest character run
	//       that is contiguous in memory, and for which all code units share
	//       the same formatting,
	//    3) *pFormatting points to the format sheared by the code units at
	//       *ppCharacters. Inheritance of formatting properties is already
	//       resolved.
	//
	//------------------------------------------------------------------------
	void GetRun(
		uint characterPosition,
		out TextFormatting? ppTextFormatting,
		out InheritedProperties? ppInheritedProperties,
		out TextNestingType pNestingType,
		out TextElement? ppNestedElement,
		out ReadOnlyMemory<char> ppCharacters,
		out uint pcCharacters);

	//------------------------------------------------------------------------
	//
	//  Method:   ITextContainer::GetText
	//
	//  Synopsis: Concatenates all text content into a single flat string.
	//
	//------------------------------------------------------------------------
	string GetText(bool insertNewlines);

	//------------------------------------------------------------------------
	//
	//  Method:   ITextContainer::GetText
	//
	//  Synopsis: Concatenates all text content between 2 offsets into a
	//            flat string.
	//
	//------------------------------------------------------------------------
	string GetText(uint iTextPosition1, uint iTextPosition2, bool insertNewlines);

	//------------------------------------------------------------------------
	//
	//  Method:   ITextContainer::GetContainingElement
	//
	//  Synopsis: Returns the CTextElement that contains a text position.
	//
	//  If the startPosition corresponds to the (reserved) start or end
	//  position of an element, that element will be returned.
	//  If the position is within the content (i.e. non-start or end position)
	//  of an element, that element returned.
	//  The containing element is always the closest contatining element,
	//  or the immediate parent of a position, not any other ancestor.
	//  For collections that reserve positions e.g. InlineCollection reserves
	//  its start/end positions, the containing element is NULL, because
	//  a collection can't be considered an element. It is the responsibility
	//  of parents of InlineCollection to handle this case.
	//
	//------------------------------------------------------------------------
	void GetContainingElement(
		uint characterPosition,
		out TextElement? ppContainingElement);

	//------------------------------------------------------------------------
	//
	//  Method:   ITextContainer::GetElementEdgeOffset
	//
	//  Synopsis: Returns the offset of the TextElement at the specified edge
	//            in the container. If the element is not found, *pFound is
	//            FALSE.
	//
	//------------------------------------------------------------------------
	void GetElementEdgeOffset(
		TextElement pElement,
		ElementEdge edge,
		out uint pOffset,
		out bool pFound);

	//------------------------------------------------------------------------
	//
	//  Method:   ITextContainer::GetOwnerUIElement
	//
	//  Synopsis: Get the owner UIElement so that we can trigger a re-layout when a font download complete
	//
	//------------------------------------------------------------------------
	UIElement? GetOwnerUIElement();
}
