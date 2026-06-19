// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichTextBlock.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

// Master-side linking machinery for the overflow chain (CRichTextBlock : ILinkedTextContainer).
// The master owns the content (TextContainer / BlockCollection) and the single TextSelectionManager;
// overflow elements render a slice of that content. This partial provides the break propagation and
// the LinkedRichTextBlockView the manager queries across the whole chain.
partial class RichTextBlock : ILinkedTextContainer
{
	private RichTextBlockOverflow? _pOverflowTarget;
	private LinkedRichTextBlockView? _pLinkedView;
	private bool _isBreakValid;

	// The per-element text view (no knowledge of linked layout). Returns the standalone RichTextBlockView.
	internal RichTextBlockView? GetSingleElementTextView() => _pTextView;

	// The view the manager queries: the LinkedRichTextBlockView when linked to an overflow chain,
	// otherwise the standalone per-element view.
	internal ITextView? GetTextView() => _pLinkedView is not null ? _pLinkedView : _pTextView;

	internal TextSelectionManager? GetSelectionManager() => _pSelectionManager;

	internal BlockLayoutEngine? GetBlockLayoutEngine() => _blockLayout;

	#region ILinkedTextContainer

	ILinkedTextContainer? ILinkedTextContainer.GetPrevious() => null;

	ILinkedTextContainer? ILinkedTextContainer.GetNext() => _pOverflowTarget;

	internal RichTextBlockOverflow? GetNext() => _pOverflowTarget;

	TextBreak? ILinkedTextContainer.GetBreak()
	{
		// In the linking implementation, another container should only request the break if it is valid.
		MUX_ASSERT(_isBreakValid);
		return _break;
	}

	internal RichTextBlockBreak? GetBreak() => _break;

	bool ILinkedTextContainer.IsBreakValid() => _isBreakValid;

	internal bool IsBreakValid() => _isBreakValid;

	Result ILinkedTextContainer.PreviousBreakUpdated(ILinkedTextContainer pPrevious)
	{
		// Master has no previous link.
		return Result.Success;
	}

	Result ILinkedTextContainer.PreviousLinkAttached(ILinkedTextContainer pPrevious)
	{
		// Master has no previous link.
		return Result.Success;
	}

	Result ILinkedTextContainer.NextLinkDetached(ILinkedTextContainer pNext)
	{
		MUX_ASSERT(ReferenceEquals(_pOverflowTarget, pNext));
		_pOverflowTarget = null;
		return Result.Success;
	}

	Result ILinkedTextContainer.PreviousLinkDetached(ILinkedTextContainer pPrevious)
	{
		// Master has no previous link.
		return Result.Success;
	}

	bool ILinkedTextContainer.IsMaster() => true;

	#endregion

	// Connects the next link and notifies it that a previous link has attached itself.
	private void ResolveNextLink()
	{
		if (_pOverflowTarget is not null)
		{
			// Create a linked view. At this stage we should not have a linked view - if the overflow target
			// changed it should have been reset when the previous target detached. If there is no linked view,
			// create one and notify the selection manager of the change.
			if (_pLinkedView is null)
			{
				_pLinkedView = new LinkedRichTextBlockView(this);
				_pSelectionManager?.TextViewChanged(_pTextView, _pLinkedView);
			}
			((ILinkedTextContainer)_pOverflowTarget).PreviousLinkAttached(this);
		}
		else
		{
			// Overflow target set to null: delete the linked view and go back to being a standalone control.
			if (_pSelectionManager is not null)
			{
				_pSelectionManager.TextViewChanged(_pLinkedView, _pTextView);
			}
			_pLinkedView = null;
		}
	}

	// Invalidates information stored about the next link. Called when the next link changes or is detached.
	private void InvalidateNextLink()
	{
		if (_pOverflowTarget is not null)
		{
			((ILinkedTextContainer)_pOverflowTarget).PreviousLinkDetached(this);
		}
		_pOverflowTarget = null;
	}

	// Records the break for this container and propagates HasOverflowContent + notifies the next link.
	private void SetBreak(RichTextBlockBreak? pBreak)
	{
		bool hasBreak = _break is not null;
		bool hasNewBreak = pBreak is not null;

		_break = pBreak;
		_isBreakValid = true;

		if (hasBreak != hasNewBreak)
		{
			// HasOverflowContent returns (_break != null); fire the change when it flips.
			HasOverflowContent = hasNewBreak;
		}

		// Notify the next link that the break has been updated.
		if (_pOverflowTarget is not null)
		{
			((ILinkedTextContainer)_pOverflowTarget).PreviousBreakUpdated(this);
		}
	}

	// Sets the overflow target field and re-resolves the linked chain. Mirrors the OverflowContentTarget
	// special-case in CRichTextBlock::SetValue (the property-system multi-association bypass) - driven from
	// the OverflowContentTarget changed callback in RichTextBlock.Properties.cs.
	partial void OnOverflowContentTargetChangedPartial(RichTextBlockOverflow oldTarget, RichTextBlockOverflow newTarget)
	{
		InvalidateNextLink();

		_pOverflowTarget = newTarget;

		ResolveNextLink();
	}

	// Propagates content invalidation through the chain. Called when the master's content changes.
	internal void InvalidateOverflowChainContent(bool clearCachedLinks)
	{
		_isBreakValid = false;
		RichTextBlockOverflow.InvalidateAllOverflowContent(_pOverflowTarget, clearCachedLinks);
	}

	internal void InvalidateOverflowChainContentMeasure()
	{
		_isBreakValid = false;
		RichTextBlockOverflow.InvalidateAllOverflowContentMeasure(_pOverflowTarget);
	}
}
