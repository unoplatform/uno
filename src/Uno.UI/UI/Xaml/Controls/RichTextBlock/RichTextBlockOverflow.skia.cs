// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichTextBlockOverflow.cpp, RichTextBlockOverflow.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.UI.Xaml.Core.Scaling;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class RichTextBlockOverflow : ILinkedTextContainer
{
	// Fields unique to RichTextBlockOverflow.
	private RichTextBlock? _pMaster;
	private ILinkedTextContainer? _pPreviousLink;

	// Fields common to RichTextBlock and RichTextBlockOverflow.
	private RichTextBlockBreak? _pBreak;
	private bool _isBreakValid;

	// Block layout state.
	private PageNode? _pPageNode;
	private RichTextBlockView? _pTextView;

	// Render data rebuilt from the arranged page node (the master's content slice that fits this page).
	private readonly List<RichTextBlock.ParagraphLayout> _paragraphLayouts = new();

	internal RichTextBlock? GetMaster() => _pMaster;

	internal PageNode? GetPageNode() => _pPageNode;

	// Returns the per-element TextView for just this element (no knowledge of linked layout).
	internal RichTextBlockView? GetSingleElementTextView() => _pTextView;

	private protected override ContainerVisual CreateElementVisual() => new RichTextBlockOverflowVisual(Compositor.GetSharedCompositor(), this);

	#region ILinkedTextContainer

	ILinkedTextContainer? ILinkedTextContainer.GetPrevious() => _pPreviousLink;

	ILinkedTextContainer? ILinkedTextContainer.GetNext() => OverflowContentTarget;

	internal RichTextBlockOverflow? GetNext() => OverflowContentTarget;

	TextBreak? ILinkedTextContainer.GetBreak()
	{
		// Another container should only request the break if it is valid.
		MUX_ASSERT(_isBreakValid);
		return _pBreak;
	}

	bool ILinkedTextContainer.IsBreakValid() => _isBreakValid;

	Result ILinkedTextContainer.PreviousBreakUpdated(ILinkedTextContainer pPrevious)
	{
		MUX_ASSERT(_pPreviousLink is not null && ReferenceEquals(_pPreviousLink, pPrevious));
		InvalidateMeasure();
		return Result.Success;
	}

	Result ILinkedTextContainer.PreviousLinkAttached(ILinkedTextContainer pPrevious)
	{
		// If there's already a previous link, notify it that this element is detaching itself.
		if (_pPreviousLink is not null)
		{
			_pPreviousLink.NextLinkDetached(this);
		}

		// Set this and all subsequent masters to null, they'll be re-evaluated when measured.
		ResetAllOverflowMasters(this);

		_pPreviousLink = pPrevious;

		InvalidateMeasure();
		return Result.Success;
	}

	Result ILinkedTextContainer.NextLinkDetached(ILinkedTextContainer pNext)
	{
		MUX_ASSERT(ReferenceEquals(OverflowContentTarget, pNext));
		OverflowContentTarget = null!;
		return Result.Success;
	}

	Result ILinkedTextContainer.PreviousLinkDetached(ILinkedTextContainer pPrevious)
	{
		MUX_ASSERT(_pPreviousLink is not null && ReferenceEquals(_pPreviousLink, pPrevious));

		// Set this and all subsequent masters to null, they'll be re-evaluated when measured.
		ResetAllOverflowMasters(this);

		_pPreviousLink = null;

		InvalidateMeasure();
		return Result.Success;
	}

	bool ILinkedTextContainer.IsMaster() => false;

	#endregion

	internal uint GetContentStartPosition()
	{
		if (_pPageNode is not null &&
			!_pPageNode.IsMeasureDirty() &&
			!_pPageNode.IsArrangeDirty())
		{
			MUX_ASSERT(_pMaster is not null);
			return _pPageNode.GetStartPosition();
		}
		return 0;
	}

	internal uint GetContentLength()
	{
		if (_pPageNode is not null &&
			!_pPageNode.IsMeasureDirty() &&
			!_pPageNode.IsArrangeDirty())
		{
			// If there is no overflow target, content extends to the end of the master's TextContainer;
			// otherwise return only the content that fits on this page.
			MUX_ASSERT(_pMaster is not null);
			if (OverflowContentTarget is null)
			{
				if (_pMaster?.Blocks.GetTextContainer() is { } container)
				{
					uint contentStart = GetContentStartPosition();
					container.GetPositionCount(out var containerLength);
					MUX_ASSERT(containerLength >= contentStart);
					return containerLength - contentStart;
				}
			}
			else
			{
				return _pPageNode.GetContentLength();
			}
		}

		return 0;
	}

	//------------------------------------------------------------------------
	//
	//  RichTextBlockOverflow::MeasureOverride
	//
	//------------------------------------------------------------------------
	protected override Size MeasureOverride(Size availableSize)
	{
		Size desiredSize = default;

		SetupLinkedBlockLayout();

		// Always use the RichTextBlockBreak object to retrieve the old page break.
		BlockNodeBreak? oldPageBreak = _pBreak?.GetBlockBreak();

		if (_pPageNode is not null)
		{
			// We shouldn't be able to create a page node without a previous link.
			MUX_ASSERT(_pPreviousLink is not null);

			if (IsPreviousBreakValid())
			{
				RichTextBlockBreak? pPreviousBreak = GetPreviousBreak();
				if (pPreviousBreak is not null)
				{
					// No RichTextBlockBreak should exist without a block break from the BlockLayoutEngine.
					MUX_ASSERT(pPreviousBreak.GetBlockBreak() is not null);
					oldPageBreak = _pPageNode.GetBreak();
					_pPageNode.Measure(availableSize, (uint)MaxLines, 0.0f, false, false, true, pPreviousBreak.GetBlockBreak(), out _);
					desiredSize = _pPageNode.GetDesiredSize();

					// If PageNode bypasses Measure its break will be exactly the same and there's no need to notify
					// overflow elements. Otherwise update the container's break and set it, notifying overflows.
					if (_pPageNode.GetBreak() is not null)
					{
						if (!ReferenceEquals(_pPageNode.GetBreak(), oldPageBreak))
						{
							SetBreak(new RichTextBlockBreak(_pPageNode.GetBreak()));
						}
					}
					else
					{
						if (oldPageBreak is not null)
						{
							SetBreak(null);
						}
					}
				}
				else
				{
					// There's no content here - delete the page node.
					_pPageNode = null;
					_pTextView = null;
					SetBreak(null);
				}
			}
		}

		if (GetUseLayoutRounding())
		{
			var plateauScale = RootScale.GetRasterizationScaleForElement(this);
			desiredSize.Width = ((int)Math.Ceiling(desiredSize.Width * plateauScale)) / plateauScale;
			desiredSize.Height = ((int)Math.Ceiling(desiredSize.Height * plateauScale)) / plateauScale;
		}

		return desiredSize;
	}

	//------------------------------------------------------------------------
	//
	//  RichTextBlockOverflow::ArrangeOverride
	//
	//------------------------------------------------------------------------
	protected override Size ArrangeOverride(Size finalSize)
	{
		Visual.Compositor.InvalidateRender(Visual);

		Size renderSize = default;

		if (_pPageNode is not null && !_pPageNode.IsMeasureDirty())
		{
			_pPageNode.Arrange(finalSize);
			renderSize = _pPageNode.GetRenderSize();
			PopulateLayoutsFromTree();
			UpdateIsTextTrimmed();
		}

		// If we lose the page node and text was previously trimmed, account for it not being trimmed anymore.
		if (_pPageNode is null && IsTextTrimmed)
		{
			SetIsTextTrimmed(false);
		}

		// Return the larger of render size and finalSize so content is positioned/clipped correctly.
		var newFinalSize = new Size(
			Math.Max(finalSize.Width, renderSize.Width),
			Math.Max(finalSize.Height, renderSize.Height));

		base.ArrangeOverride(finalSize);
		return newFinalSize;
	}

	//------------------------------------------------------------------------
	//
	//  RichTextBlockOverflow::SetupLinkedBlockLayout
	//
	//  Queries the master for the block layout engine necessary for layout.
	//
	//------------------------------------------------------------------------
	private void SetupLinkedBlockLayout()
	{
		// If there is no existing page node, use the master's block layout engine to create one.
		if (_pPageNode is null)
		{
			if (_pMaster is null &&
				_pPreviousLink is not null)
			{
				// The previous link either has or is a master. Don't ever look further than the previous link.
				if (_pPreviousLink.IsMaster())
				{
					_pMaster = (RichTextBlock)_pPreviousLink;
				}
				else
				{
					_pMaster = ((RichTextBlockOverflow)_pPreviousLink).GetMaster();
				}
			}

			if (_pMaster is not null &&
				_pMaster.GetBlockLayoutEngine() is { } engine &&
				_pMaster.Blocks is not null)
			{
				_pPageNode = (PageNode)engine.CreatePageNode(_pMaster.Blocks, this);
			}
		}

		// If there is a valid PageNode there is content that will be laid out. Create a TextView.
		if (_pPageNode is not null &&
			_pTextView is null)
		{
			_pTextView = new RichTextBlockView(_pPageNode, this);
		}
	}

	private bool IsPreviousBreakValid() => _pPreviousLink?.IsBreakValid() ?? false;

	private RichTextBlockBreak? GetPreviousBreak() => _pPreviousLink?.GetBreak() as RichTextBlockBreak;

	//------------------------------------------------------------------------
	//
	//  RichTextBlockOverflow::SetBreak
	//
	//------------------------------------------------------------------------
	private void SetBreak(RichTextBlockBreak? pBreak)
	{
		bool hasBreak = _pBreak is not null;
		bool hasNewBreak = pBreak is not null;

		_pBreak = pBreak;
		_isBreakValid = true;

		if (hasBreak != hasNewBreak)
		{
			// HasOverflowContent returns (_pBreak != null); fire the change when it flips.
			SetHasOverflowContent(hasNewBreak);
		}

		// Notify the next link that the break has been updated.
		if (OverflowContentTarget is not null)
		{
			((ILinkedTextContainer)OverflowContentTarget).PreviousBreakUpdated(this);
		}
	}

	//------------------------------------------------------------------------
	//
	//  RichTextBlockOverflow::UpdateIsTextTrimmed
	//
	//------------------------------------------------------------------------
	private void UpdateIsTextTrimmed()
	{
		bool isTrimmed = false;

		for (var blockChild = _pPageNode?.GetFirstChild(); blockChild is not null; blockChild = blockChild.GetNext())
		{
			if (blockChild is ParagraphNode paragraphNode && paragraphNode.GetHasTrimmedLine())
			{
				isTrimmed = true;
				break;
			}
		}

		SetIsTextTrimmed(isTrimmed);
	}

	// Rebuilds _paragraphLayouts (render data) from the arranged page node. The overflow has no Blocks of
	// its own; its page node measures a slice of the master's BlockCollection starting at GetFirstChildIndex().
	// Mirrors RichTextBlock.PopulateLayoutsFromTree, mapping each ParagraphNode back to the master's Blocks slice.
	private void PopulateLayoutsFromTree()
	{
		_paragraphLayouts.Clear();

		if (_pMaster is null || _pPageNode is null)
		{
			return;
		}

		float accumHeight = 0;
		int blockIndex = (int)_pPageNode.GetFirstChildIndex();
		int blockCount = _pMaster.Blocks.Count;
		int globalCharOffset = (int)_pPageNode.GetStartPosition();

		for (var child = _pPageNode.GetFirstChild(); child is not null; child = child.GetNext())
		{
			var para = blockIndex < blockCount ? _pMaster.Blocks[blockIndex] as Paragraph : null;

			if (child is ParagraphNode paragraphNode &&
				para is not null &&
				paragraphNode.GetParsedText() is { } parsed)
			{
				var margin = para.Margin;
				var boxSize = paragraphNode.GetDesiredSize(); // content + margins
				var contentSize = new Size(
					Math.Max(0, boxSize.Width - margin.Left - margin.Right),
					Math.Max(0, boxSize.Height - margin.Top - margin.Bottom));

				_paragraphLayouts.Add(new RichTextBlock.ParagraphLayout(
					ParsedText: parsed,
					YOffset: accumHeight + (float)margin.Top,
					GlobalCharOffset: globalCharOffset,
					Size: contentSize,
					Margin: margin));

				accumHeight += (float)boxSize.Height;
			}

			if (para is not null)
			{
				globalCharOffset += string.Concat(para.Inlines.Select(InlineExtensions.GetText)).Length;
				if (blockIndex < blockCount - 1)
				{
					globalCharOffset += 2; // paragraph separator
				}
			}

			blockIndex++;
		}
	}

	internal void Draw(in Visual.PaintingSession session)
	{
		var canvas = session.Canvas;
		canvas.Save();
		canvas.Translate((float)Padding.Left, (float)Padding.Top);

		foreach (var layout in _paragraphLayouts)
		{
			canvas.Save();
			canvas.Translate((float)layout.Margin.Left, layout.YOffset);

			// TODO Uno (Stage 9 overflow selection): apply selection/text-highlighters to the overflow's
			// content slice (the master's TextSelectionManager owns selection across the whole chain).
			layout.ParsedText.Draw(session, null, Enumerable.Empty<TextHighlighter>(), compositionRange: null);

			canvas.Restore();
		}

		canvas.Restore();
	}

	// the entire body of the overflow is considered hit-testable
	internal override bool HitTest(Point point)
	{
		var transform = GetTransform(this, (UIElement)this.GetParent());
		var success = Matrix3x2.Invert(transform, out var inverted);
		return success && inverted.Transform(LayoutSlotWithMarginsAndAlignments).Contains(point);
	}

	/// <summary>
	/// Skia Visual for RichTextBlockOverflow rendering.
	/// </summary>
	internal sealed class RichTextBlockOverflowVisual : ContainerVisual
	{
		private readonly WeakReference<RichTextBlockOverflow> _owner;

		public RichTextBlockOverflowVisual(Compositor compositor, RichTextBlockOverflow owner) : base(compositor)
		{
			_owner = new WeakReference<RichTextBlockOverflow>(owner);
		}

		internal override void Paint(in PaintingSession session)
		{
			if (_owner.TryGetTarget(out var owner))
			{
				owner.Draw(in session);
			}
		}

		internal override bool CanPaint() => true;
	}
}
