// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextCollapsingCharacters.h, TextCollapsingCharacters.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

// The collapsing symbol (ellipsis) shown when a TextLine is collapsed by text
// trimming. TODO Uno (Stage 6 / risk R2): the ellipsis glyph run is net-new on Uno
// (no equivalent in the Skia engine yet); stubbed until line collapsing is ported.
internal sealed class TextCollapsingCharacters : TextCollapsingSymbol
{
	// TODO Uno (Stage 6): the C++ ctor takes the collapsing symbol metrics; stubbed for now.
	public TextCollapsingCharacters(object? a = null, object? b = null, object? c = null, object? d = null, object? e = null, object? f = null, object? g = null) { }

	public override double Width
		=> throw new NotSupportedException("TODO Uno (Stage 6): line collapsing / ellipsis is not yet ported.");

	public override void Draw(TextDrawingContext drawingContext, Point origin, double viewportWidth, FlowDirection flowDirection)
		=> throw new NotSupportedException("TODO Uno (Stage 6): line collapsing / ellipsis is not yet ported.");
}
