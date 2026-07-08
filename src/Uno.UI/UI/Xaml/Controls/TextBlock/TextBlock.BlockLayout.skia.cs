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
	// Uno renders TextBlock through the kept ParsedText engine rather than the ported PageNode,
	// so the baseline is read from the first formatted line and offset by the top padding (which
	// the WinUI block-layout branch folds into PageNode.GetBaselineAlignmentOffset).
	internal void GetBaselineOffset(out float pBaselineOffset)
		=> pBaselineOffset = ParsedText.FirstLineBaseline + (float)Padding.Top;
}
