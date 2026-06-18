// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BlockNode.h, BlockNode.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

// MarginCollapsingState is just the accumulated bottom margin carried between blocks.
using MarginCollapsingState = global::System.Single;

internal abstract class BlockNode
{
	protected BlockLayoutEngine m_pBlockLayoutEngine;
	protected DependencyObject? m_pElement;
	protected ContainerNode? m_pParentNode;

	// NOTE: desiredSize, renderSize and prevAvailableSize do not contain margin value.
	// Internally all computation is done without adding margin, however public getters
	// for those properties add collapsed margin when returning values to the caller.
	protected Size m_desiredSize;
	protected Size m_renderSize;
	protected Size m_prevAvailableSize;
	protected uint m_cachedMaxLines; // used to check CanBypassMeasure()
	protected uint m_measuredLines;

	protected BlockNodeBreak? m_pBreak;
	protected BlockNodeBreak? m_pPreviousBreak;
	protected uint m_length;
	protected DrawingContext? m_pDrawingContext;
	protected Thickness m_margin;

	private BlockNode? m_pNext;
	private BlockNode? m_pPrevious;

	// Layout state "flags".
	private bool m_isContentDirty;
	private bool m_isMeasureDirty;
	private bool m_isArrangeDirty;
	private bool m_isMeasureInProgress;
	private bool m_isArrangeInProgress;
	private bool m_isDrawInProgress;
	private bool m_isEmptyContentAllowed;
	private bool m_measureBottomless;
	private bool m_isMeasureBypassed;
	private bool m_isArrangeBypassed;

	protected BlockNode(
		BlockLayoutEngine pBlockLayoutEngine,
		DependencyObject? pElement,
		ContainerNode? pParentNode)
	{
		m_pBlockLayoutEngine = pBlockLayoutEngine;
		m_pElement = pElement;
		m_pParentNode = pParentNode;
		m_pBreak = null;
		m_pPreviousBreak = null;
		m_pNext = null;
		m_pPrevious = null;
		m_length = 0;
		m_pDrawingContext = null;
		m_isContentDirty = false;
		m_isMeasureDirty = true;
		m_isArrangeDirty = true;
		m_isMeasureInProgress = false;
		m_isArrangeInProgress = false;
		m_isDrawInProgress = false;
		m_isEmptyContentAllowed = false;
		m_measureBottomless = false;
		m_isMeasureBypassed = false;
		m_isArrangeBypassed = false;
		m_cachedMaxLines = 0;
		m_measuredLines = 0;

		MUX_ASSERT(pBlockLayoutEngine != null);

		m_desiredSize = new Size(0, 0);
		m_renderSize = new Size(0, 0);
		m_prevAvailableSize = new Size(float.PositiveInfinity, float.PositiveInfinity);
		m_margin = default;
	}

	public Result Measure(
		Size availableSize,
		uint maxLines,
		MarginCollapsingState mcsIn,
		bool allowEmptyContent,
		bool measureBottomless,
		bool suppressTopMargin,
		BlockNodeBreak? pPreviousBreak,
		out MarginCollapsingState mcsOut)
	{
		var canBypassMeasure = false;

		// BlockLayoutEngine doesn't support Measure during Measure.
		MUX_ASSERT(!IsMeasureInProgress());

		// Perform margin collapsing.
		// Left and Right margin values are untouched.
		// Bottom margin is always set to 0, since it is handled during margin collapsing by the next block.
		// Top margin is set to maximum of previous value from margin collapsing state and the currently applied margin.
		// However, if this is the first block on the page (suppressTopMargin is true), margin is always set to 0.
		BlockLayoutHelpers.GetBlockMargin(m_pElement, out var margin);
		m_margin.Left = margin.Left;
		m_margin.Right = margin.Right;
		m_margin.Top = suppressTopMargin ? 0.0 : Math.Max(margin.Top, mcsIn);
		m_margin.Bottom = 0.0;
		mcsOut = (float)margin.Bottom;

		// Extract collapsed margin values from the availableSize.
		availableSize.Width = Math.Max(0.0, availableSize.Width - (m_margin.Left + m_margin.Right));
		availableSize.Height = Math.Max(0.0, availableSize.Height - (m_margin.Top + m_margin.Bottom));

		canBypassMeasure = CanBypassMeasure(availableSize, maxLines, allowEmptyContent, measureBottomless, pPreviousBreak);

		m_prevAvailableSize = availableSize;
		m_pPreviousBreak = pPreviousBreak;
		m_isEmptyContentAllowed = allowEmptyContent;
		m_measureBottomless = measureBottomless;

		// BlockNode will execute MeasureCore if:
		// 1. Measure was marked dirty/invalid.
		// 2. Measure was not marked dirty, but cannot be bypassed as determined by bypass checks implemented by the node.
		if (!IsMeasureInProgress() &&
			!canBypassMeasure)
		{
			m_isMeasureInProgress = true;

			try
			{
				MeasureCore(availableSize,
							maxLines,
							allowEmptyContent,
							measureBottomless,
							suppressTopMargin,
							pPreviousBreak);
			}
			finally
			{
				m_isMeasureInProgress = false;
			}
		}

		// Invalidating content invalidates measure, and content dirty can be reset here.
		m_isContentDirty = false;
		m_isMeasureDirty = false;
		m_isMeasureBypassed = canBypassMeasure;

		return Result.Success;
	}

	public void Arrange(Size finalSize)
	{
		var canBypassArrange = false;

		// BlockLayoutEngine doesn't support Arrange during Arrange.
		MUX_ASSERT(!IsArrangeInProgress());

		// Extract collapsed margin values from the availableSize.
		finalSize.Width = Math.Max(0.0, finalSize.Width - (m_margin.Left + m_margin.Right));
		finalSize.Height = Math.Max(0.0, finalSize.Height - (m_margin.Top + m_margin.Bottom));

		// Arrange cannot be bypassed if:
		// 1. Arrange was marked dirty/invalid.
		// 2. Arrange was not marked dirty, but cannot be bypassed as determined by bypass checks implemented by the node.
		// 3. Measure was not bypassed. Anything that was measured must be arranged. m_isMeasureBypassed tracks this
		// internally instead of using the public InvalidateArrange call which propagates invalidations to children, etc.
		canBypassArrange = CanBypassArrange(finalSize) && m_isMeasureBypassed;

		if (!IsArrangeInProgress() &&
			!canBypassArrange)
		{
			m_isArrangeInProgress = true;

			try
			{
				ArrangeCore(finalSize);
			}
			finally
			{
				m_isArrangeInProgress = false;
			}
		}

		m_isArrangeDirty = false;
		m_isArrangeBypassed = canBypassArrange;
	}

	public void Draw(bool forceDraw)
	{
		// BlockLayoutEngine doesn't support Render during Render or any other stage of layout.
		MUX_ASSERT(!IsMeasureInProgress() &&
			!IsArrangeInProgress() &&
			!IsDrawInProgress());

		// Render is only bypassed if Arrange was. There is no independent bypass for Render.
		// Re-rendering may be required in some cases even when Arrange is bypassed, e.g. high contrast selection.
		// Hence the forceRender flag.
		// Additionally, Measure and Arrange must always be valid.
		if (!IsDrawInProgress() &&
			!IsMeasureDirty() &&
			!IsArrangeDirty() &&
			(forceDraw || !m_isArrangeBypassed))
		{
			m_isDrawInProgress = true;

			try
			{
				// Content rendering offset has been already calculated during layout.
				// Apply this offset to the drawing context to position rendered content appropriately.
				var contentRenderingOffset = GetContentRenderingOffset();
				m_pDrawingContext?.SetTransform(contentRenderingOffset);

				DrawCore(forceDraw);
				m_isArrangeBypassed = true;
			}
			finally
			{
				m_isDrawInProgress = false;
			}
		}
	}

	public virtual float GetBaselineAlignmentOffset() => throw new NotImplementedException();

	public void InvalidateContent()
	{
		if (!IsMeasureInProgress())
		{
			m_isContentDirty = true;
			m_isMeasureDirty = true;
			m_isArrangeDirty = true;
			var pChildNode = GetFirstChild();
			while (pChildNode != null)
			{
				pChildNode.InvalidateContent();
				pChildNode = pChildNode.GetNext();
			}
		}
	}

	public void InvalidateMeasure()
	{
		if (!IsMeasureInProgress())
		{
			m_isMeasureDirty = true;
			m_isArrangeDirty = true;
			var pChildNode = GetFirstChild();
			while (pChildNode != null)
			{
				pChildNode.InvalidateMeasure();
				pChildNode = pChildNode.GetNext();
			}
		}
	}

	public void InvalidateArrange()
	{
		if (!IsArrangeInProgress())
		{
			m_isArrangeDirty = true;
			var pChildNode = GetFirstChild();
			while (pChildNode != null)
			{
				pChildNode.InvalidateArrange();
				pChildNode = pChildNode.GetNext();
			}
		}
	}

	// Transforms coordinates from root-relative to block-relative i.e. from the
	// coordinate system of the highest-level block parent, usually PageNode, to
	// the block's coordinate system.
	public Point TransformOffsetFromRoot(Point offset)
	{
		var origin = new Point(0.0, 0.0);

		origin = TransformOffsetToRoot(origin);

		// Subtract the origin's transformed offset from the value passed in to get the relative offset.
		// Results are MAXed with 0 so we don't return a negative point, but don't bother to MIN with desired size, etc.
		// This is an internal API so we expect callers to be reasonable with input.
		return new Point(
			Math.Max(offset.X - origin.X, 0.0),
			Math.Max(offset.Y - origin.Y, 0.0));
	}

	// Transforms coordinates from block-relative to root-relative i.e. to the
	// coordinate system of the highest-level block parent, usually PageNode.
	public Point TransformOffsetToRoot(Point offset)
	{
		var contentOffset = GetContentRenderingOffset();
		var transformedOffset = new Point(offset.X + contentOffset.X, offset.Y + contentOffset.Y);
		var pParent = m_pParentNode;
		BlockNode pChild = this;

		while (pParent != null)
		{
			var childOffset = pParent.GetChildOffset(pChild);
			transformedOffset.X += childOffset.X;
			transformedOffset.Y += childOffset.Y;
			pChild = pParent;
			pParent = pParent.m_pParentNode;
		}

		return transformedOffset;
	}

	public bool IsMeasureDirty() => m_isMeasureDirty;

	public bool IsArrangeDirty() => m_isArrangeDirty;

	// Base BlockNode has no children, return null here.
	public virtual BlockNode? GetFirstChild() => null;

	public BlockNode? GetNext() => m_pNext;

	public BlockNode? GetPrevious() => m_pPrevious;

	public void SetNext(BlockNode? pNext) => m_pNext = pNext;

	public void SetPrevious(BlockNode? pPrevious) => m_pPrevious = pPrevious;

	public Size GetDesiredSize() => new Size(
		m_desiredSize.Width + (m_margin.Left + m_margin.Right),
		m_desiredSize.Height + (m_margin.Top + m_margin.Bottom));

	public Size GetRenderSize() => new Size(
		m_renderSize.Width + (m_margin.Left + m_margin.Right),
		m_renderSize.Height + (m_margin.Top + m_margin.Bottom));

	public uint GetContentLength() => m_length;

	public BlockNodeBreak? GetBreak() => m_pBreak;

	public DrawingContext? GetDrawingContext() => m_pDrawingContext;

	// Returns the pixel offset at which content is rendered at this node. This is
	// not an absolute offset or parent-relative offset. Those offsets are handled
	// by the parent. This offset determines where content is drawn within the
	// block's coordinate space and is meant to account for padding, border, etc.
	public virtual Point GetContentRenderingOffset() => new Point(m_margin.Left, m_margin.Top);

	// Query methods used by TextView.
	public abstract bool IsAtInsertionPosition(uint position);
	public abstract uint PixelPositionToTextPosition(Point pixel, out TextGravity gravity);
	public abstract void GetTextBounds(uint start, uint length, List<TextBounds> pBounds);

	public FlowDirection GetFlowDirection() => BlockLayoutHelpers.GetFlowDirection(m_pBlockLayoutEngine.GetOwner());

	// The method to clean up all the device related realizations on this subtree.
	public void CleanupRealizations()
	{
		var pChild = GetFirstChild();
		while (pChild != null)
		{
			pChild.CleanupRealizations();
			pChild = pChild.GetNext();
		}
		m_pDrawingContext?.CleanupRealizations();
	}

	public uint GetMeasuredLinesCount() => m_measuredLines;

	protected abstract void MeasureCore(
		Size availableSize,
		uint maxLines,
		bool allowEmptyContent,
		bool measureBottomless,
		bool suppressTopMargin,
		BlockNodeBreak? pPreviousBreak);

	protected abstract void ArrangeCore(Size finalSize);

	protected abstract void DrawCore(bool forceDraw);

	// Virtual layout bypass checks to allow derived classes to implement their own layout bypass policies.
	//
	// BlockNode is the base class and performs the most conservative layout bypass check since it
	// can't make any assumptions about layout behavior - strict equality of all parameters.
	protected virtual bool CanBypassMeasure(
		Size availableSize,
		uint maxLines,
		bool allowEmptyContent,
		bool measureBottomless,
		BlockNodeBreak? pPreviousBreak)
	{
		var bypass = false;
		if (!IsMeasureDirty() &&
			(IsEmptyContentAllowed() == allowEmptyContent) &&
			(IsMeasureBottomless() == measureBottomless) &&
			BlockLayoutHelpers.IsCloseReal(m_prevAvailableSize.Width, availableSize.Width) &&
			(m_cachedMaxLines == maxLines) &&
			BlockNodeBreak.Equals(pPreviousBreak, m_pPreviousBreak))
		{
			if (m_pBreak != null)
			{
				// If there is a break for this paragraph, then height constraint needs to be
				// the same to break at the same place.
				bypass = BlockLayoutHelpers.IsCloseReal(availableSize.Height, m_prevAvailableSize.Height);
			}
			else
			{
				// If there was no break, we can bypass as long as the desired height will fit
				// in the available space.
				bypass = (m_desiredSize.Height <= availableSize.Height);
			}
		}

		return bypass;
	}

	protected bool CanBypassArrange(Size finalSize)
	{
		// BlockNode is the base class and performs the most conservative layout bypass check since it
		// can't make any assumptions about layout behavior - strict equality of all parameters.
		return (!IsArrangeDirty() &&
			BlockLayoutHelpers.IsCloseReal(m_renderSize.Width, finalSize.Width) &&
			BlockLayoutHelpers.IsCloseReal(m_renderSize.Height, finalSize.Height));
	}

	// Protected data about the state of layout that can be used by overrides to get layout information.
	protected bool IsContentDirty() => m_isContentDirty;
	protected bool IsMeasureInProgress() => m_isMeasureInProgress;
	protected bool IsArrangeInProgress() => m_isArrangeInProgress;
	protected bool IsDrawInProgress() => m_isDrawInProgress;
	protected bool IsEmptyContentAllowed() => m_isEmptyContentAllowed;
	protected bool IsMeasureBottomless() => m_measureBottomless;
}
