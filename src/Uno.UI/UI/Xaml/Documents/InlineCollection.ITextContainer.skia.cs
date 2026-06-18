// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference InlineCollection.cpp, inline.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using Uno.UI.Extensions;

namespace Microsoft.UI.Xaml.Documents;

// The run-model members (GetPositionCount/GetRun/GetText/GetContainingElement/
// GetElementEdgeOffset) are defined as `internal` instance methods in
// InlineCollection.TextContainer.skia.cs (Stage 4a). They're surfaced on ITextContainer
// here via explicit interface implementations that forward to them (an `internal` method
// cannot implicitly satisfy a public interface member). This partial also adds the three
// remaining ITextContainer members.
partial class InlineCollection : RichTextServices.ITextContainer
{
	//  CInlineCollection::GetCore — forwarding to DependencyObject.GetContext().
	//  (InlineCollection is not itself a DependencyObject in Uno; the backing
	//  DependencyObjectCollection carries the context.)
	Uno.UI.Xaml.Core.CoreServices RichTextServices.ITextContainer.GetCore() => _collection.GetContext();

	//  CInlineCollection::GetAsDependencyObject — returns the backing collection,
	//  which is the DependencyObject that owns the inlines.
	DependencyObject RichTextServices.ITextContainer.GetAsDependencyObject() => _collection;

	void RichTextServices.ITextContainer.GetPositionCount(out uint pcPositions)
		=> GetPositionCount(out pcPositions);

	void RichTextServices.ITextContainer.GetRun(
		uint characterPosition,
		out RichTextServices.TextFormatting? ppTextFormatting,
		out InheritedProperties? ppInheritedProperties,
		out TextNestingType pNestingType,
		out TextElement? ppNestedElement,
		out ReadOnlyMemory<char> ppCharacters,
		out uint pcCharacters)
		=> GetRun(
			characterPosition,
			out ppTextFormatting,
			out ppInheritedProperties,
			out pNestingType,
			out ppNestedElement,
			out ppCharacters,
			out pcCharacters);

	string RichTextServices.ITextContainer.GetText(bool insertNewlines)
		=> GetText(insertNewlines);

	string RichTextServices.ITextContainer.GetText(uint iTextPosition1, uint iTextPosition2, bool insertNewlines)
		=> GetText(iTextPosition1, iTextPosition2, insertNewlines);

	void RichTextServices.ITextContainer.GetContainingElement(
		uint characterPosition,
		out TextElement? ppContainingElement)
		=> GetContainingElement(characterPosition, out ppContainingElement);

	void RichTextServices.ITextContainer.GetElementEdgeOffset(
		TextElement pElement,
		ElementEdge edge,
		out uint pOffset,
		out bool pFound)
		=> GetElementEdgeOffset(pElement, edge, out pOffset, out pFound);

	//------------------------------------------------------------------------
	//
	//  Method:   CInlineCollection::GetOwnerUIElement
	//
	//  Synopsis: return the owner UIElement.
	//            used to trigger a re-layout when a font download complete
	//
	//------------------------------------------------------------------------
	UIElement? RichTextServices.ITextContainer.GetOwnerUIElement() => GetParentInternal() as UIElement;
}
