// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PageNodeBreak.h, PageNodeBreak.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

/// <summary>
/// Contains state at the point where a PageNode is broken.
/// </summary>
internal sealed class PageNodeBreak : BlockNodeBreak
{
	// Block break for this page break.
	private readonly BlockNodeBreak? m_pBlockBreak;

	// PageNode is based on a BlockCollection. PageNodeBreak indicates the index in
	// the collection at which PageNode should start formatting. If a BlockBreak is
	// also included, then the formatting is partially completed on a previous page.
	private readonly uint m_blockIndexInCollection;

	public PageNodeBreak(
		uint breakIndex,
		uint blockIndexInCollection,
		BlockNodeBreak? pBlockBreak)
		: base(breakIndex)
	{
		m_blockIndexInCollection = blockIndexInCollection;
		m_pBlockBreak = pBlockBreak;
	}

	public BlockNodeBreak? BlockBreak => m_pBlockBreak;

	public uint BlockIndexInCollection => m_blockIndexInCollection;

	// BlockNodeBreak::Equals override for page breaks. Checks that the break's
	// absolute index, block index and block break matches this object's.
	public override bool Equals(BlockNodeBreak? pBreak)
	{
		var equals = false;
		var pPageBreak = pBreak as PageNodeBreak;

		if (pPageBreak is not null)
		{
			equals = (pPageBreak.BreakIndex == BreakIndex) &&
				(pPageBreak.BlockIndexInCollection == BlockIndexInCollection) &&
				Equals(pPageBreak.BlockBreak, BlockBreak);
		}

		return equals;
	}
}
