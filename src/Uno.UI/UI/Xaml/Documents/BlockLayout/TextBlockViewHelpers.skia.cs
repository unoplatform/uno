// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBlockViewHelpers.h, TextBlockViewHelpers.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

// Static helpers shared by the text views (RichTextBlockView / TextBlockView) for
// mapping embedded elements to text positions.
internal static class TextBlockViewHelpers
{
	// Walks the inline collection accumulating the text offset and returns true (updating
	// positionOfIUC) when the InlineUIContainer is found.
	// TODO Uno (Stage 6): faithful port lands with the text-view layer (run model / GetRun).
	public static bool FindIUCPositionInInlines(InlineCollection inlines, InlineUIContainer iuc, ref uint positionOfIUC)
		=> throw new NotSupportedException("TODO Uno (Stage 6): TextBlockViewHelpers.FindIUCPositionInInlines is not yet ported.");
}
