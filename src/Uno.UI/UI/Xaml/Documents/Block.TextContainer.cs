// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BlockTextElement.h (CBlock), tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

using System;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents;

partial class Block
{
	// CBlock::GetRun — Implemented in derived classes of Block (Paragraph).
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
		throw new InvalidOperationException(); // Implemented in derived classes of Block.
	}

	// CBlock::GetElementEdgeOffset — Implemented in derived classes of Block (Paragraph).
	internal virtual void GetElementEdgeOffset(
		TextElement pElement,
		ElementEdge edge,
		out uint pOffset,
		out bool pFound)
	{
		MUX_ASSERT(false);
		throw new InvalidOperationException(); // Implemented in derived classes of Block.
	}
}
