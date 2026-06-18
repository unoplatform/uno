// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ParagraphNodeBreak.h, ParagraphNodeBreak.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Microsoft.UI.Xaml.Documents.RichTextServices;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

/// <summary>
/// Contains state at the point where a ParagraphNode is broken.
/// </summary>
internal sealed class ParagraphNodeBreak : BlockNodeBreak
{
	// Line breaking information for the last formatted line if it exists.
	private readonly TextLineBreak? m_pLineBreak;

	// Run cache.
	private readonly TextRunCache m_pRunCache;

	public ParagraphNodeBreak(
		uint breakIndex,
		TextLineBreak? pLineBreak,
		TextRunCache pRunCache)
		: base(breakIndex)
	{
		m_pLineBreak = pLineBreak;
		m_pRunCache = pRunCache;
	}

	// Gets the line break where the paragraph was broken.
	public TextLineBreak? LineBreak => m_pLineBreak;

	// Gets the run cache for the paragraph.
	public TextRunCache RunCache => m_pRunCache;

	// BlockNodeBreak::Equals override for paragraph breaks. Checks that the break's
	// paragraph-relative index matches this object's.
	public override bool Equals(BlockNodeBreak? pBreak)
	{
		var equals = false;
		var pParagraphBreak = pBreak as ParagraphNodeBreak;

		if (pParagraphBreak is not null)
		{
			// Index equality is enough here, if any actual content changed that is processed
			// through content changed handlers and Measure will be invalidated on affected
			// nodes, negating bypass.
			equals = pParagraphBreak.BreakIndex == BreakIndex;
		}

		return equals;
	}
}
