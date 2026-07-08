// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichTextBlockOverflow.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class RichTextBlockOverflow
{
	/// <summary>
	/// Gets the distance between the top of the <see cref="RichTextBlockOverflow"/> and the
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

	// CRichTextBlockOverflow::GetBaselineOffset
	//
	// Synopsis: Gets distance to baseline of first line from top of control.
	internal void GetBaselineOffset(out float pBaselineOffset)
	{
		pBaselineOffset = 0;
		if (_pPageNode != null)
		{
			pBaselineOffset = _pPageNode.GetBaselineAlignmentOffset();
		}
	}
}
