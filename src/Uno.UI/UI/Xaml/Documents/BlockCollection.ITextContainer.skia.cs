// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BlockCollection.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using Uno.UI.Extensions;

namespace Microsoft.UI.Xaml.Documents;

// The run-model members (GetPositionCount/GetRun/GetContainingElement/
// GetElementEdgeOffset/CacheLengths) are defined as `internal` instance methods in
// BlockCollection.TextContainer.skia.cs (Stage 4a). They're surfaced on ITextContainer
// here via explicit interface implementations that forward to them (an `internal` method
// cannot implicitly satisfy a public interface member). This partial also defines the
// GetText overloads (absent from 4a) and the three remaining ITextContainer members.
partial class BlockCollection : RichTextServices.ITextContainer
{
	//  CBlockCollection::GetCore — forwarding to DependencyObject.GetContext().
	Uno.UI.Xaml.Core.CoreServices RichTextServices.ITextContainer.GetCore() => this.GetContext();

	//  CBlockCollection::GetAsDependencyObject — BlockCollection is itself a DependencyObject.
	DependencyObject RichTextServices.ITextContainer.GetAsDependencyObject() => this;

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

	UIElement? RichTextServices.ITextContainer.GetOwnerUIElement() => GetOwnerUIElement();

	//------------------------------------------------------------------------
	//
	//  Method:   CBlockCollection::GetText
	//
	//  Synopsis: Concatenates all text content into a single flat string.
	//
	//------------------------------------------------------------------------
	internal string GetText(bool insertNewlines)
	{
		GetPositionCount(out var length);
		return GetText(0, length, insertNewlines);
	}

	//------------------------------------------------------------------------
	//
	//  Method:   CBlockCollection::GetText
	//
	//  Synopsis: Concatenates all text content between 2 offsets into a flat string.
	//
	//  NOTE (Uno): WinUI delegated to CTextBoxHelpers::GetText, which walks the run
	//  model between the two positions. That helper is not yet ported; the run-model
	//  walk below reproduces its text-concatenation behavior over GetRun.
	//
	//------------------------------------------------------------------------
	internal string GetText(uint iTextPosition1, uint iTextPosition2, bool insertNewlines)
	{
		GetPositionCount(out var cPositions);

		if (iTextPosition2 > cPositions)
		{
			iTextPosition2 = cPositions;
		}

		var builder = new global::System.Text.StringBuilder();
		uint position = iTextPosition1;

		while (position < iTextPosition2)
		{
			GetRun(
				position,
				out _,
				out _,
				out _,
				out _,
				out var characters,
				out var cCharacters);

			if (cCharacters == 0)
			{
				break;
			}

			if (!characters.IsEmpty)
			{
				uint take = Math.Min(cCharacters, iTextPosition2 - position);
				var span = characters.Span;
				builder.Append(span.Slice(0, (int)Math.Min(take, (uint)span.Length)));
			}

			position += cCharacters;
		}

		return builder.ToString();
	}

	//------------------------------------------------------------------------
	//
	//  Method:   CBlockCollection::GetOwnerUIElement
	//
	//  Synopsis: return the owner UIElement.
	//            used to trigger a re-layout when a font download complete
	//
	//------------------------------------------------------------------------
	internal UIElement? GetOwnerUIElement() => this.GetParentInternal(false) as UIElement;
}
