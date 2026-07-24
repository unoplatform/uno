// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference InlineUIContainer.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents;

partial class InlineUIContainer
{
	// CInlineUIContainer does not override GetPositionCount; it inherits CTextElement's default of
	// 2 (Open/Close). GetRun below validates characterPosition <= 2 / rejects >= 2 accordingly.
	internal override void GetRun(
		uint characterPosition,
		out RichTextServices.TextFormatting? ppTextFormatting,
		out InheritedProperties? ppInheritedProperties,
		out TextNestingType pNestingType,
		out TextElement? ppNestedElement,
		out ReadOnlyMemory<char> ppCharacters,
		out uint pcCharacters)
	{
		// InlineUIContainer only has 2 positions - Open/Close.
		MUX_ASSERT(characterPosition <= 2);
		if (characterPosition >= 2)
		{
			throw new ArgumentOutOfRangeException(nameof(characterPosition));
		}

		ppCharacters = ReadOnlyMemory<char>.Empty;
		pcCharacters = 1;

		GetTextFormatting(out ppTextFormatting);
		GetInheritedProperties(out ppInheritedProperties);

		ppNestedElement = this;

		pNestingType = (characterPosition == 0) ? TextNestingType.OpenNesting : TextNestingType.CloseNesting;
	}
}
