// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichTextBlockBreak.h, RichTextBlockBreak.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Microsoft.UI.Xaml.Documents.RichTextServices;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

//---------------------------------------------------------------------------
//
//  RichTextBlockBreak
//
//  Contains state at the point where a CRichTextBlock or CRichTextBlockOverflow
//  has stopped formatting due to breaking process.
//
//---------------------------------------------------------------------------
internal class RichTextBlockBreak : TextBreak
{
	// Information about where the last block was broken if it was partially formatted.
	private readonly BlockNodeBreak? m_pBlockBreak;

	// Initializes instance of RichTextBlockBreak.
	public RichTextBlockBreak(BlockNodeBreak? pBlockBreak)
	{
		m_pBlockBreak = pBlockBreak;
	}

	public BlockNodeBreak? GetBlockBreak() => m_pBlockBreak;
}
