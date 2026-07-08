// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBlock.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class TextBlock
{
	/// <summary>
	/// Gets the distance between the top of the <see cref="TextBlock"/> and the
	/// baseline of its first line of text.
	/// </summary>
	public double BaselineOffset
	{
		get
		{
			GetBaselineOffset(out var baselineOffset);
			return baselineOffset;
		}
	}

	// CTextBlock::GetBaselineOffset
	//
	// Synopsis: Gets distance to baseline of first line from top of control.
	//
	// A plain TextBlock takes WinUI's default TextMode::DWriteLayout branch, which returns the first
	// line's baseline straight from the layout metrics — measured from the content box, so padding is
	// NOT included (WinUI adds padding separately, e.g. in GetActualHeight). Uno's kept ParsedText
	// engine measures the same content box, so FirstLineBaseline matches directly (and returns 0
	// before the first measure, matching WinUI's m_hasBeenMeasured guard).
	internal void GetBaselineOffset(out float pBaselineOffset)
		=> pBaselineOffset = ParsedText.FirstLineBaseline;
}
