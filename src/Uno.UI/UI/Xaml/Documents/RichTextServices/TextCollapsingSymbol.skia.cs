// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextCollapsingSymbol.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// TextCollapsingSymbol contains properties and methods used to format and
/// display the symbol shown when a TextLine is collapsed.
/// </summary>
internal abstract class TextCollapsingSymbol
{
	// Gets the width of the collapsing symbol.
	public abstract double Width { get; }

	// Creates rendering data for the symbol.
	public abstract void Draw(
		TextDrawingContext drawingContext,
		Point origin,
		double viewportWidth,
		FlowDirection flowDirection);
}
