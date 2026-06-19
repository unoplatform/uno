// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichTextBlockOverflow.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Microsoft.UI.Xaml.Documents.RichTextServices;

namespace Microsoft.UI.Xaml.Controls;

// Chain-walking invalidation / selection-notify helpers for the linked overflow chain. These mirror the
// static CRichTextBlockOverflow helpers that propagate down the m_pOverflowTarget chain non-recursively.
partial class RichTextBlockOverflow
{
	// Mirrors the OverflowContentTarget special-case in CRichTextBlockOverflow::SetValue: detach the old next
	// link, set the new one, and re-resolve. Driven from the OverflowContentTarget changed callback.
	partial void OnOverflowContentTargetChangedPartial(RichTextBlockOverflow oldTarget, RichTextBlockOverflow newTarget)
	{
		// Notify the next link that the previous link (this) has been detached, then attach the new one.
		if (oldTarget is not null)
		{
			((ILinkedTextContainer)oldTarget).PreviousLinkDetached(this);
		}

		if (newTarget is not null)
		{
			((ILinkedTextContainer)newTarget).PreviousLinkAttached(this);
		}

		// Switching the target between null and non-null may affect paragraph-ellipsis (trimming) rendering;
		// invalidate arrange so it can be updated. Breaks are never affected.
		InvalidateContentArrange();
		InvalidateArrange();
	}

	//------------------------------------------------------------------------
	//
	//  RichTextBlockOverflow::InvalidateContent
	//
	//------------------------------------------------------------------------
	private void InvalidateContent(bool clearCachedLinks)
	{
		// Invalidate content for the block layout engine, which invalidates all layout as well.
		_pPageNode?.InvalidateContent();

		// TODO Uno (Stage 9 overflow): clearCachedLinks resets the cached hyperlink hit-test state
		// (m_currentLink / m_pressedHyperlink) once overflow hyperlink hit-testing is wired.

		InvalidateMeasure();
	}

	//------------------------------------------------------------------------
	//
	//  RichTextBlockOverflow::InvalidateContentMeasure
	//
	//------------------------------------------------------------------------
	private void InvalidateContentMeasure()
	{
		// Invalidate measure for the block layout engine, which invalidates arrange as well.
		_pPageNode?.InvalidateMeasure();

		_isBreakValid = false;

		InvalidateMeasure();
	}

	//------------------------------------------------------------------------
	//
	//  RichTextBlockOverflow::InvalidateContentArrange
	//
	//------------------------------------------------------------------------
	private void InvalidateContentArrange()
	{
		_pPageNode?.InvalidateArrange();

		InvalidateArrange();
	}

	//------------------------------------------------------------------------
	//
	//  RichTextBlockOverflow::ResetMaster
	//
	//  Resets master and deletes the existing page node, since it was created
	//  using the previous master's BLE.
	//
	//------------------------------------------------------------------------
	private void ResetMaster()
	{
		_pMaster = null;
		_pPageNode = null;
		_pTextView = null;
	}

	// Static helpers: propagate invalidation/reset/selection through the full chain of overflow links,
	// starting at pFirst, non-recursively.

	internal static void InvalidateAllOverflowContent(RichTextBlockOverflow? pFirst, bool clearCachedLinks)
	{
		for (var pOverflow = pFirst; pOverflow is not null; pOverflow = pOverflow.OverflowContentTarget)
		{
			pOverflow.InvalidateContent(clearCachedLinks);
		}
	}

	internal static void InvalidateAllOverflowContentMeasure(RichTextBlockOverflow? pFirst)
	{
		for (var pOverflow = pFirst; pOverflow is not null; pOverflow = pOverflow.OverflowContentTarget)
		{
			pOverflow.InvalidateContentMeasure();
		}
	}

	internal static void InvalidateAllOverflowContentArrange(RichTextBlockOverflow? pFirst)
	{
		for (var pOverflow = pFirst; pOverflow is not null; pOverflow = pOverflow.OverflowContentTarget)
		{
			pOverflow.InvalidateContentArrange();
		}
	}

	internal static void InvalidateAllOverflowRender(RichTextBlockOverflow? pFirst)
	{
		for (var pOverflow = pFirst; pOverflow is not null; pOverflow = pOverflow.OverflowContentTarget)
		{
			pOverflow.Visual.Compositor.InvalidateRender(pOverflow.Visual);
		}
	}

	private static void ResetAllOverflowMasters(RichTextBlockOverflow? pFirst)
	{
		for (var pOverflow = pFirst; pOverflow is not null; pOverflow = pOverflow.OverflowContentTarget)
		{
			pOverflow.ResetMaster();
		}
	}

	// Selection changed notification from the master's TextSelectionManager. Determines whether this element
	// is affected by the change (newly/previously selected) and invalidates its render if so.
	// TODO Uno (Stage 9 overflow selection): drive these from the master's OnSelectionChanged once the manager
	// notifies the whole chain (NotifyAllOverflowContentSelectionChanged); selection rendering for the slice
	// is not yet wired into Draw.
	internal void OnSelectionChanged(
		uint previousSelectionStartOffset,
		uint previousSelectionEndOffset,
		uint newSelectionStartOffset,
		uint newSelectionEndOffset)
	{
		uint contentStart = GetContentStartPosition();
		uint contentEnd = contentStart + GetContentLength();

		bool selected = newSelectionStartOffset < contentEnd && newSelectionEndOffset >= contentStart;
		bool unSelected = previousSelectionStartOffset < contentEnd && previousSelectionEndOffset >= contentStart;

		if (selected || unSelected)
		{
			Visual.Compositor.InvalidateRender(Visual);
		}
	}

	internal void OnSelectionVisibilityChanged(uint selectionStartOffset, uint selectionEndOffset)
	{
		uint contentStart = GetContentStartPosition();
		uint contentEnd = contentStart + GetContentLength();

		if (selectionStartOffset < contentEnd && selectionEndOffset >= contentStart)
		{
			Visual.Compositor.InvalidateRender(Visual);
		}
	}
}
