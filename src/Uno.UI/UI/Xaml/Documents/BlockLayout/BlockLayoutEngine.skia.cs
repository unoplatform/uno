// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BlockLayoutEngine.h, BlockLayoutEngine.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable
#pragma warning disable CS8600, CS8602, CS8604, CS8618, CS0219, CS0414 // TODO Uno (Stage 5): WIP drafts not yet fully nullable-annotated

using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

//---------------------------------------------------------------------------
//
//  BlockLayoutEngine
//
//---------------------------------------------------------------------------
internal sealed class BlockLayoutEngine
{
	private readonly DependencyObject m_pOwner;

	//---------------------------------------------------------------------------
	//
	// BlockLayoutEngine::BlockLayoutEngine
	//
	//---------------------------------------------------------------------------
	public BlockLayoutEngine(
		DependencyObject pOwner)
	{
		m_pOwner = pOwner;
		MUX_ASSERT(m_pOwner != null);
	}

	//---------------------------------------------------------------------------
	//
	// BlockLayoutEngine::~BlockLayoutEngine
	//
	//---------------------------------------------------------------------------
	// NOTE (Uno): the C++ destructor was empty; nothing to dispose under the GC.

	//---------------------------------------------------------------------------
	//
	//  BlockLayoutEngine::CreatePageNode
	//
	//  Synopsis:
	//      Creates a page node that represents the root of block layout for its
	//      owner.
	//
	//---------------------------------------------------------------------------
	public BlockNode CreatePageNode(
		BlockCollection? pBlocks,
		FrameworkElement pPageOwner)
	{
		if (pBlocks == null)
		{
			// Block collection may be NULL or empty - NULL for TextBlock, empty for a content-less RichTextBlock.
			// TODO Uno (integrate): OfTypeByIndex<KnownTypeIndex::TextBlock> -> "m_pOwner is TextBlock".
			MUX_ASSERT(m_pOwner is TextBlock);
		}

		// PageOwner should never be NULL, page can read padding and other properties from it.
		// TODO Uno (integrate): IFCCATASTROPHIC_RETURN(pPageOwner) — fail fast when the page owner is null.
		MUX_ASSERT(pPageOwner != null);

		return new PageNode(this, pBlocks, pPageOwner);
	}

	public DependencyObject GetOwner() => m_pOwner;
}
