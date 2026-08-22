// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LineMetricsCache.h, LineMetricsCache.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

//---------------------------------------------------------------------------
//
//  Line Metrics Cache
//
//     Holds cached information about lines that were laid out for
//     display after an arrange pass.
//
//     This information can be used for speeding up hit testing/incremental
//     layout.
//
//---------------------------------------------------------------------------
internal struct LineMetricsCache
{
	// layout box for the line
	private Rect m_layoutBox;

	// length of characters in the line
	private uint m_length;

	// Number of characters taken by any trailing newline
	private uint m_newlineLength;

	private float m_baseline;

	private bool m_hasMultiCharacterClusters;

	//------------------------------------------------------------------------
	//  Summary:
	//      Constructor
	//------------------------------------------------------------------------
	public LineMetricsCache()
	{
		m_layoutBox = new Rect(0.0, 0.0, 0.0, 0.0);

		m_length = 0;
		m_newlineLength = 0;
		m_baseline = 0.0f;

		m_hasMultiCharacterClusters = false;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Sets primary properties of the cache.
	//------------------------------------------------------------------------
	public void Set(
		Rect layoutBox,
		uint length,
		uint newlineLength,
		float baseline,
		bool hasMultiCharacterClusters)
	{
		m_layoutBox = layoutBox;
		m_length = length;
		m_newlineLength = newlineLength;
		m_baseline = baseline;
		m_hasMultiCharacterClusters = hasMultiCharacterClusters;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Get layout box.
	//------------------------------------------------------------------------
	public readonly Rect GetLayoutBox() => m_layoutBox;

	//------------------------------------------------------------------------
	//  Summary:
	//      Get length of line.
	//------------------------------------------------------------------------
	public readonly uint GetLength() => m_length;

	//------------------------------------------------------------------------
	//  Summary:
	//      Get the number of characters used by any newline.
	//      0: Line has no newline, for example broken on a hyphen.
	//      1: Line end with a one character newline, such as LF.
	//      2: Line ends with a two character newline, such as CR/LF.
	//------------------------------------------------------------------------
	public readonly uint GetNewlineLength() => m_newlineLength;

	//------------------------------------------------------------------------
	//  Summary:
	//      Get the baseline of the line.
	//------------------------------------------------------------------------
	public readonly float GetBaseline() => m_baseline;

	//------------------------------------------------------------------------
	//  Summary:
	//      Set alignment offset.
	//------------------------------------------------------------------------
	public void SetOffset(Point alignmentOffset)
	{
		m_layoutBox.X = alignmentOffset.X;
		m_layoutBox.Y = alignmentOffset.Y;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Returns whether the line contains multi-character clusters.
	//      See TextLine::HasMultiCharacterClusters.
	//------------------------------------------------------------------------
	public readonly bool HasMultiCharacterClusters() => m_hasMultiCharacterClusters;
}
