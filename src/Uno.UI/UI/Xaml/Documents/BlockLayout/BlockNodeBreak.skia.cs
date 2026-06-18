// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BlockNodeBreak.h, BlockNodeBreak.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Microsoft.UI.Xaml.Documents.RichTextServices;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

/// <summary>
/// Contains state at the point where a block is broken.
/// </summary>
internal class BlockNodeBreak : TextBreak
{
	// Index of break relative to block start.
	private readonly uint m_breakIndex;

	protected BlockNodeBreak(uint breakIndex)
	{
		m_breakIndex = breakIndex;
	}

	public uint BreakIndex => m_breakIndex;

	// Equality operator - used in layout bypass.
	// Overridden by derived classes based on the contents of their break records.
	//
	// Base class implementation must be the strictest check lest it causes
	// unintended layout bypass, so this implementation is just reference equals.
	public virtual bool Equals(BlockNodeBreak? pBreak)
	{
		return pBreak == this;
	}

	// Static helper for equality checking so callers can avoid NULL checks, etc.
	public static bool Equals(BlockNodeBreak? pFirst, BlockNodeBreak? pSecond)
	{
		return (pFirst is null && pSecond is null) ||
			(pFirst is not null &&
			 pSecond is not null &&
			 pFirst.Equals(pSecond));
	}
}
