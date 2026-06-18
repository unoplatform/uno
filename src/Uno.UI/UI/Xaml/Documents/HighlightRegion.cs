// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference HighlightRegion.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Documents;

internal class HighlightRegion
{
	// The start and end indexes of a HighlightRegion take a position,
	// which may include both visible characters and hidden offsets,
	// as opposed to a character index, which would only count visible characters.
	public HighlightRegion(
		int startIndexInit,
		int endIndexInit,
		SolidColorBrush? foregroundBrushInit,
		SolidColorBrush? backgroundBrushInit)
	{
		StartIndex = startIndexInit;
		EndIndex = endIndexInit;
		ForegroundBrush = foregroundBrushInit;
		BackgroundBrush = backgroundBrushInit;
	}

	public int StartIndex { get; set; }
	public int EndIndex { get; set; }
	public SolidColorBrush? ForegroundBrush { get; set; }
	public SolidColorBrush? BackgroundBrush { get; set; }
}
