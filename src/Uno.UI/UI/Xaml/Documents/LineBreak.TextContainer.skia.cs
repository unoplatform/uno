// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LineBreak.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents;

partial class LineBreak
{
	//------------------------------------------------------------------------
	//
	//  Method:   CLineBreak::GetRun
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
		// LineBreak only has 2 positions - Open/Close.
		MUX_ASSERT(characterPosition <= 2);
		if (characterPosition >= 2)
		{
			throw new ArgumentOutOfRangeException(nameof(characterPosition));
		}

		if (characterPosition == 0)
		{
			ppCharacters = ReadOnlyMemory<char>.Empty;
		}
		else
		{
			ppCharacters = "\x2028".AsMemory();
		}
		pcCharacters = 1;

		// LineBreak only has 2 positions. The first position is treated as a hidden run with no characters and a count of 1.
		GetTextFormatting(out ppTextFormatting);
		GetInheritedProperties(out ppInheritedProperties);

		ppNestedElement = this;

		pNestingType = (characterPosition == 0) ? TextNestingType.OpenNesting : TextNestingType.NestedContent;
	}
}
