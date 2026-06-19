// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PageNode.h, PageNode.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable
#pragma warning disable CS8600, CS8602, CS8604, CS8618, CS0219, CS0414 // TODO Uno (Stage 5): WIP drafts not yet fully nullable-annotated

using System;
using Uno.UI.Extensions;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

//---------------------------------------------------------------------------
//
//  PageNode
//
//  Represents one page of content.
//
//---------------------------------------------------------------------------
internal sealed class PageNode : ContainerNode
{
	// Uno integration helpers replacing C++ idioms (delete / IFC).
	private static void DeleteBlockNode(BlockNode? node) { /* GC; C++ 'delete pChildNode' */ }
	private static void ThrowOnRtsError(global::Microsoft.UI.Xaml.Documents.RichTextServices.Result rtsErr)
	{
		if (rtsErr != global::Microsoft.UI.Xaml.Documents.RichTextServices.Result.Success)
		{
			throw new global::System.InvalidOperationException($"RichTextServices error: {rtsErr}");
		}
	}

	// CFrameworkElement that acts as the container for all embedded elements processed by this PageNode.
	// This need not be the same as the element that owns the BlockCollection that is PageNode's content,
	// but it is where any embedded elements encountered during formatting are attached.
	private FrameworkElement m_pPageOwner;

	// Struct used by PageNode to store information about embedded elements obtained during different stages of formatting.
	private struct EmbeddedElementInfo
	{
		public InlineUIContainer pContainer;
		public ParagraphNode pParagraphNode;
		public Point position;
		public bool isVisible;
	}

	private List<EmbeddedElementInfo> m_embeddedElements = new();

	// Cached index of this page's first child in the block collection. This is used to speed up calculations in
	// Measure when there's an incoming BreakRecord - we can compare the start index against the break record index
	// and delete children if necessary, etc. If content is invalidated all children will be deleted and PageNode will
	// ignore cached start index value in this case.
	// TODO: Consider setting it to some default value e.g. -1 in InvalidateContent override. Not necessary
	// now since can just check if first child == NULL, but may be necessary with incremental layout.
	private uint m_firstChildIndex;

	public PageNode(
		BlockLayoutEngine pBlockLayoutEngine,
		BlockCollection? pBlocks,
		FrameworkElement pPageOwner)
		: base(pBlockLayoutEngine, pBlocks, null)
	{
		m_pPageOwner = pPageOwner;
		m_firstChildIndex = 0;
	}

	// ~PageNode override - ClearEmbeddedElements() is called on disposal.
	// TODO Uno (integrate): C++ destructor calls ClearEmbeddedElements(); wire this into the
	// lead's BlockNode disposal/cleanup path (e.g. CleanupRealizations / IDisposable).

	//---------------------------------------------------------------------------
	//
	// PageNode::AddElement
	//
	//  Synopsis:
	//      Adds the given embedded element to the host.
	//
	//---------------------------------------------------------------------------
	public void AddElement(
		InlineUIContainer pContainer,
		ParagraphNode pParagraphNode)
	{
		EmbeddedElementInfo info = new() { pContainer = pContainer, pParagraphNode = pParagraphNode, position = new Point(0.0f, 0.0f), isVisible = false /* isVisible */ };

		// If the element already exists, fail.
		if (FindElement(pContainer, pParagraphNode))
		{
			throw new ArgumentException(); // E_INVALIDARG
		}

		// TODO Uno (integrate): CInlineUIContainer::GetChild - returns the hosted UIElement child.
		UIElement pElement = pContainer.GetChild();

		// Reparent the element if it is not parented by this page's container.
		if (pElement.GetParentInternal() != m_pPageOwner)
		{
			// TODO Uno (integrate): CFrameworkElement::AddChild
			m_pPageOwner.AddChild(pElement);
		}

		m_embeddedElements.Add(info);
	}

	//---------------------------------------------------------------------------
	//
	//  PageNode::RemoveElement
	//
	//  Synopsis:
	//      Removes the given embedded element from the host.
	//
	//---------------------------------------------------------------------------
	public void RemoveElement(
		InlineUIContainer pContainer,
		ParagraphNode pParagraphNode)
	{
		uint elementIndex = 0;

		if (!FindElement(pContainer, pParagraphNode, out _, out elementIndex))
		{
			throw new ArgumentException(); // E_INVALIDARG
		}

		RemoveElement(pContainer, elementIndex);
	}

	//---------------------------------------------------------------------------
	//
	//  PageNode::RemoveElement
	//
	//  Synopsis:
	//      Removes the given embedded element from the host.
	//
	//---------------------------------------------------------------------------
	public void RemoveElement(
		InlineUIContainer pContainer,
		uint elementIndex)
	{
		// TODO Uno (integrate): CInlineUIContainer::GetChild
		UIElement pElement = pContainer.GetChild();
		// TODO Uno (integrate): CFrameworkElement::RemoveChild
		m_pPageOwner.RemoveChild(pElement);
		m_embeddedElements.RemoveAt((int)elementIndex);

		// Reset the container's cached host, since it has been removed from this page's hosted elements
		// collection it should not remember the page.
		// TODO Uno (integrate): CInlineUIContainer::ClearCachedHost
		pContainer.ClearCachedHost();
	}

	//---------------------------------------------------------------------------
	//
	//  PageNode::UpdateElementPosition
	//
	//  Synopsis:
	//      Updates the given embedded element position, IEmbeddedElementHost override.
	//
	//---------------------------------------------------------------------------
	public void UpdateElementPosition(
		InlineUIContainer pContainer,
		ParagraphNode pParagraphNode,
		Point position)
	{
		uint elementIndex = 0;
		EmbeddedElementInfo elementInfo;

		if (!FindElement(pContainer, pParagraphNode, out elementInfo, out elementIndex))
		{
			throw new ArgumentException(); // E_INVALIDARG
		}

		elementInfo.position = position;
		elementInfo.isVisible = true;

		m_embeddedElements[(int)elementIndex] = elementInfo;
	}

	//---------------------------------------------------------------------------
	//
	//  PageNode::GetElementPosition
	//
	//  Synopsis:
	//      Retrieves the embedded element position in host space, IEmbeddedElementHost override.
	//
	//---------------------------------------------------------------------------
	public Point GetElementPosition(
		InlineUIContainer pContainer,
		ParagraphNode pParagraphNode)
	{
		EmbeddedElementInfo elementInfo;

		if (!FindElement(pContainer, pParagraphNode, out elementInfo))
		{
			throw new ArgumentException(); // E_INVALIDARG
		}

		return elementInfo.position;
	}

	//---------------------------------------------------------------------------
	//
	//  PageNode::GetElementsWithinRange
	//
	//  Synopsis:
	//      Retrieves the array of embedded elements within two text positions.
	//
	//---------------------------------------------------------------------------
	public List<InlineUIContainer> GetElementsWithinRange(
		uint position1,
		uint position2)
	{
		List<InlineUIContainer> ppChildren;
		uint size = (uint)m_embeddedElements.Count;
		uint textPosition;
		List<EmbeddedElementInfo> embeddedElementsInRange = new();
		EmbeddedElementInfo elementInfo;
		uint count = 0;

		for (uint i = 0; i < size; ++i)
		{
			elementInfo = m_embeddedElements[(int)i];
			textPosition = FindInlineUIContainerOffset(elementInfo.pContainer);
			if (textPosition >= position1 && textPosition <= position2)
			{
				embeddedElementsInRange.Add(elementInfo);
			}
		}

		count = (uint)embeddedElementsInRange.Count;
		ppChildren = new List<InlineUIContainer>((int)count);
		for (uint i = 0; i < count; i++)
		{
			elementInfo = embeddedElementsInRange[(int)i];
			ppChildren.Add(elementInfo.pContainer);
		}

		return ppChildren;
	}

	public uint FindInlineUIContainerOffset(InlineUIContainer iuc)
	{
		uint positionOfIUC = 0;
		bool found = false;
		BlockCollection pBlocks = (BlockCollection)m_pElement;

		DependencyObject? previousBlock = null;
		// TODO Uno (integrate): CBlockCollection::GetCollection - underlying ordered block list.
		var blockCollection = pBlocks.GetCollection();
		foreach (var block in blockCollection)
		{
			// Account for the offset at the end of each paragraph before searching the next one
			if (previousBlock is not null)
			{
				positionOfIUC += 2;
			}
			// Go through each collection of inlines and try to find the InlineUIContainer we're looking for
			// TODO Uno (integrate): CParagraph::GetInlineCollection
			var inlines = ((Paragraph)block).GetInlineCollection();
			// TODO Uno (integrate): TextBlockViewHelpers::FindIUCPositionInInlines - walks inlines accumulating
			// text offset, returns true (and updates positionOfIUC) when the InlineUIContainer is found.
			bool iucFound = TextBlockViewHelpers.FindIUCPositionInInlines(inlines, iuc, /* Inout */ ref positionOfIUC);
			if (iucFound)
			{
				found = true;
				// Don't need to search through any other paragraphs
				break;
			}
			previousBlock = block;
		}

		MUX_ASSERT(found);
		return positionOfIUC;
	}

	//------------------------------------------------------------------------
	//
	//  PageNode::RemoveParagraphEmbeddedElements
	//
	//  Synopsis:
	//      Removes the embedded elements associated with the specified
	//      ParagraphNode.
	//
	//------------------------------------------------------------------------
	public void RemoveParagraphEmbeddedElements(
		ParagraphNode pParagraphNode)
	{
		InlineUIContainer? pContainer = null;
		UIElement? pElement = null;
		uint size = (uint)m_embeddedElements.Count;
		uint index = 0;
		EmbeddedElementInfo elementInfo;

		while (index < size)
		{
			elementInfo = m_embeddedElements[(int)index];
			if (elementInfo.pParagraphNode == pParagraphNode)
			{
				pContainer = elementInfo.pContainer;
				// TODO Uno (integrate): CInlineUIContainer::GetChild
				pElement = pContainer.GetChild();
				// TODO Uno (integrate): CFrameworkElement::RemoveChild
				m_pPageOwner.RemoveChild(pElement);
				// TODO Uno (integrate): CInlineUIContainer::ClearCachedHost
				pContainer.ClearCachedHost();
				pElement = null;
				m_embeddedElements.RemoveAt((int)index);
				size = (uint)m_embeddedElements.Count;
			}
			else
			{
				index++;
			}
		}
	}

	//------------------------------------------------------------------------
	//
	//  PageNode::MarkParagraphEmbeddedElementsInvisible
	//
	//  Synopsis:
	//      Marks the embedded elements associated with the specified
	//      ParagraphNode as not visible.
	//
	//------------------------------------------------------------------------
	public void MarkParagraphEmbeddedElementsInvisible(
		ParagraphNode pParagraphNode)
	{
		// Reset visible state on all embedded elements since any visible ones will be arranged.
		for (int it = 0; it < m_embeddedElements.Count; ++it)
		{
			if (m_embeddedElements[it].pParagraphNode == pParagraphNode)
			{
				var info = m_embeddedElements[it];
				info.isVisible = false;
				m_embeddedElements[it] = info;
			}
		}
	}

	public override Point GetContentRenderingOffset()
	{
		Point offset = new(0.0f, 0.0f);
		Thickness padding;

		// TODO Uno (integrate): BlockLayoutHelpers::GetPagePadding
		BlockLayoutHelpers.GetPagePadding(m_pPageOwner, out padding);
		offset.X = (float)padding.Left;
		offset.Y = (float)padding.Top;

		float layoutRoundingHeightAdjustment = 0.0f;
		// TODO Uno (integrate): BlockLayoutHelpers::GetLayoutRoundingHeightAdjustment
		BlockLayoutHelpers.GetLayoutRoundingHeightAdjustment(m_pPageOwner, out layoutRoundingHeightAdjustment);
		offset.Y += layoutRoundingHeightAdjustment;

		return offset;
	}

	//---------------------------------------------------------------------------
	//
	//  PageNode::OnChildDesiredSizeChanged
	//
	//  Synopsis:
	//      Handle desired size changes in embedded elements..
	//
	//---------------------------------------------------------------------------
	public void OnChildDesiredSizeChanged(UIElement pChild)
	{
		// If an embedded element's desired size changed, we need to invalidate measure entirely
		// since the position/wrapping of other content may also have changed.
		InvalidateMeasure();
	}

	protected override void MeasureCore(
		Size availableSize,
		uint blockMaxLines,
		bool allowEmptyContent,
		bool measureBottomless,
		bool suppressTopMargin,
		BlockNodeBreak? pPreviousBreak)
	{
		BlockNode? pChildNode = null;
		BlockNode? pLastChildNode = null;
		BlockNode? pNewFirstChild = null;
		BlockNode? pFirstChildToDelete = null;
		uint childIndexInCollection = 0;
		BlockCollection? pBlocks = m_pElement as BlockCollection;
		Size remainingSize = availableSize;
		uint availableLinesCount = blockMaxLines;
		PageNodeBreak? pPreviousPageBreak = null;
		BlockNodeBreak? pChildBreak = null;
		PageNodeBreak? pBreak = null;
		bool measuringExistingChildren = false;
		uint firstChildIndex = 0;
		uint previousBreakIndex = 0;
		Thickness padding;
		float mcs = 0.0f; // MarginCollapsingState
		bool finishMeasureWithoutBreak = false;

		if (IsContentDirty())
		{
			// ContentDirty always implies MeasureDirty.
			MUX_ASSERT(IsMeasureDirty());
			// If content is dirty, all children should be deleted and re-created, since the block collection
			// itself may have changed, so child nodes are not valid. Also clear all embedded elements.
			ClearChildren(m_pFirstChild);
			m_pFirstChild = null;
			m_firstChildIndex = 0;
			ClearEmbeddedElements();
		}
		else if (IsMeasureDirty())
		{
			ClearEmbeddedElements();
		}

		// Adjust the size to account for padding.
		// TODO Uno (integrate): BlockLayoutHelpers::GetPagePadding
		BlockLayoutHelpers.GetPagePadding(m_pPageOwner, out padding);
		remainingSize.Width = Math.Max(0.0f, (float)(remainingSize.Width - (padding.Left + padding.Right)));
		remainingSize.Height = Math.Max(0.0f, (float)(remainingSize.Height - (padding.Top + padding.Bottom)));

		m_desiredSize.Width = 0.0f;
		m_desiredSize.Height = 0.0f;
		m_length = 0;
		m_cachedMaxLines = blockMaxLines;
		m_measuredLines = 0;
		firstChildIndex = m_firstChildIndex;
		m_pBreak = null;

		if (pPreviousBreak is not null)
		{
			pPreviousPageBreak = (PageNodeBreak)pPreviousBreak;
			if (pPreviousPageBreak is not null)
			{
				// Store the character offset of the previous break, it needs to be aggregated with the length
				// of this node if this node breaks.
				childIndexInCollection = pPreviousPageBreak.BlockIndexInCollection;
				pChildBreak = pPreviousPageBreak.BlockBreak;
				previousBreakIndex = pPreviousPageBreak.BreakIndex;
			}
		}

		// Main Measure loop has the following cases, with or without break. The break simply determines the index of the first child we'll measure.
		// 1. New set of children to be measured has no overlap with existing collection and falls entirely after it, i.e:
		//    (m_firstChildIndex + child count from previous measure) < childIndexInCollection.
		//    Delete all the old children, then measure new starting at childIndexInCollection.
		// 2. New set of children to be measured has some overlap with the existing collection, but starts after it, i.e.:
		//    (m_firstChildIndex  < childIndexInCollection) but (m_firstChildIndex + child count from previous measure) > childIndexInCollection.
		//    Delete existing children up to the point of overlap, then measure existing children, then measure new if we run out of existing children to measure.
		// 3. New set of children to be measured has some overlap with the existing collection, but starts before it, i.e.:
		//    (childIndexInCollection < m_firstChildIndex) but (childIndexInCollection + new post-measure child count) > m_firstChildIndex.
		//    Measure new children until we hit m_firstChildIndex, then measure from the existing collection, then measure any additional new content if we run out of existing children to measure.
		// 4. New set of children to be measured has no overlap with the existing collection, and falls entirely before it, i.e.:
		//    (childIndexInCollection + new post-measure child count) <= m_firstChildIndex.
		//    Measure the new collection, then delete the entire old collection and replace it with the new one when we don't detect overlap.
		if (m_pFirstChild is not null)
		{
			if (firstChildIndex <= childIndexInCollection)
			{
				// Cases 1 and 2. Clear everything up to the new start index and see if there are any children left.
				BlockNode? pStart = m_pFirstChild;
				while (pStart is not null &&
						firstChildIndex < childIndexInCollection)
				{
					pChildNode = pStart;
					pStart = pStart.GetNext();
					// TODO Uno (integrate): C++ "delete pChildNode" - destroy/dispose the BlockNode.
					DeleteBlockNode(pChildNode);
					firstChildIndex++;
				}

				// The first child should now match the index specified through a break, if any.
				m_pFirstChild = pStart;
				firstChildIndex = childIndexInCollection;

				if (m_pFirstChild is not null)
				{
					m_pFirstChild.SetPrevious(null);

					// If m_pFirstChild != NULL, we're in case 2 - some overlap. Otherwise, we're in case 1 and will just measure new content.
					measuringExistingChildren = true;
				}

				// We've already intersected our overlapping region and deleted any existing children that are excluded.
				// Additional children to delete will be determined by breaking, assume we won't delete any.
				pFirstChildToDelete = null;
			}
			else
			{
				// So far we've detected no overlap. We'll start measuring new content, and assume we should delete
				// the old content unless something changes.
				pFirstChildToDelete = m_pFirstChild;
			}
		}

		pChildNode = null;
		do
		{
			// Since remainingSize.height is adjusted during the Measure loop, ensure that it never goes below 0 unless the loop is broken.
			MUX_ASSERT(remainingSize.Height >= 0.0f);

			if (measuringExistingChildren)
			{
				// Overlapping children - fetch from existing collection.
				MUX_ASSERT(childIndexInCollection >= firstChildIndex);

				if (childIndexInCollection == firstChildIndex)
				{
					pChildNode = m_pFirstChild;
				}
				else
				{
					// We should have measured at least one child.
					MUX_ASSERT(pChildNode is not null);
					pChildNode = pChildNode!.GetNext();
				}
			}
			else
			{
				// Nodes that were not covered in a previous measure. Create new.
				pChildNode = GetChildNode(pBlocks, childIndexInCollection);
			}

			if (pChildNode is not null)
			{
				// TODO Uno (integrate): BlockNode::Measure returns a RichTextServices result code (out mcs).
				var rtsErr = pChildNode.Measure(remainingSize, availableLinesCount, mcs, allowEmptyContent, measureBottomless, suppressTopMargin, pChildBreak, out mcs);
				if (rtsErr == RichTextServices.Result.INTERNAL_ERROR)
				{
					// If LineServices couldn't format the paragraph due to too many diacritics,
					// treat the paragraph as empty and swallow the error.
					allowEmptyContent = true;
				}
				else
				{
					// TODO Uno (integrate): IFC(rtsErr) - propagate non-success RichTextServices results as throw.
					ThrowOnRtsError(rtsErr);
				}

				// In ForceContent mode, a child will have non-0 content length if it is the only node on the page.
				// But a child may have 0 content length if remaining size does not allow any content and there is already content on the page.
				// In that case, we don't want to add the child to our children collection.
				if (pChildNode.GetContentLength() > 0)
				{
					Size childDesiredSize = pChildNode.GetDesiredSize();
					uint paragraphMeasuredLinesCount = pChildNode.GetMeasuredLinesCount();
					m_length += pChildNode.GetContentLength();
					m_desiredSize.Width = Math.Max(m_desiredSize.Width, childDesiredSize.Width);
					m_desiredSize.Height += childDesiredSize.Height;
					remainingSize.Height -= childDesiredSize.Height;
					m_measuredLines += paragraphMeasuredLinesCount;
					pChildBreak = pChildNode.GetBreak();

					if (blockMaxLines != 0)
					{
						MUX_ASSERT(availableLinesCount >= paragraphMeasuredLinesCount);
						availableLinesCount -= paragraphMeasuredLinesCount;
					}

					if (!measuringExistingChildren)
					{
						// If not measuring existing children, this is a brand new node that needs to be added to our children collection.
						if (pLastChildNode is not null)
						{
							pLastChildNode.SetNext(pChildNode);
							pChildNode.SetPrevious(pLastChildNode);
						}
					}
					else
					{
						if (pChildNode == m_pFirstChild &&
							pLastChildNode is not null)
						{
							// When we transition from measuring new to existing we need to connect the first
							// child to the end of the chain. At this point, set pFirstChildToDelete to NULL
							// since it means we don't want to delete the first child.
							pLastChildNode.SetNext(pChildNode);
							pChildNode.SetPrevious(pLastChildNode);
							pFirstChildToDelete = null;
						}
					}

					// Track the first node we measure as the new first node.
					if (pLastChildNode is null)
					{
						pNewFirstChild = pChildNode;
					}
					pLastChildNode = pChildNode;

					// Check if PageNode meets any criteria to end measure loop. PageNode may
					// or may not create PageNodeBreak to end measure since PageNodeBreak
					// indicates that HasContentOverflow == true. We need to do an additional check
					// when ChildNode does not create a break because there maybe additional
					// CParagarph nodes we need to measure.
					//
					// 1) Child created a break, create PageNodeBreak
					// 2) Child did not create a break and there is more content to measure
					//     a) PageNode reached maxLine or maxHeight, create PageNodeBreak
					//     b) PageNode did not reach the restrictions, continue measuring
					// 3) Child did not create a break and measured all content, exit measure
					//    loop without setting a break.
					bool createBreak = false;
					if (pChildBreak is not null)
					{
						createBreak = true;
					}
					else if (pBlocks is not null && pBlocks.GetCount() > (childIndexInCollection + 1))
					{
						childIndexInCollection++;

						if (remainingSize.Height <= 0.0f ||
							(blockMaxLines != 0 && availableLinesCount <= 0))
						{
							createBreak = true;
						}
					}
					else
					{
						finishMeasureWithoutBreak = true;
					}

					if (createBreak)
					{
						pBreak = new PageNodeBreak(m_length + previousBreakIndex,
															childIndexInCollection,
															pChildBreak);
					}

					// If we are exiting measure loop and we have measured existing children,
					// reset pFirstChildToDelete to the first child after this one for cleanup.
					if ((pBreak is not null || finishMeasureWithoutBreak) &&
						measuringExistingChildren &&
						pChildNode.GetNext() is not null)
					{
						pFirstChildToDelete = pChildNode.GetNext();
					}

					// If we measured a child with any content at all we can allow subsequent children to have empty content.
					// Also, we don't need to suppress the top margin anymore.
					allowEmptyContent = true;
					suppressTopMargin = false;
				}
				else
				{
					// If we get to the point where we measured a child, and it had no content, we should be allowing empty
					// content. The only way a page can have empty content if allowEmptyContent was initially FALSE is if we're
					// at the end of content.
					MUX_ASSERT(allowEmptyContent);

					// No content fit in this child, break the page.
					pBreak = new PageNodeBreak(m_length + previousBreakIndex,
													  childIndexInCollection,
													  pChildBreak);

					if (measuringExistingChildren)
					{
						// Delete list starts at this child.
						pFirstChildToDelete = pChildNode;
					}
					else
					{
						// TODO Uno (integrate): C++ "delete pChildNode"
						DeleteBlockNode(pChildNode);
						pChildNode = null;
					}
				}

				// After this child is measured, evaluate whether our state of measuring new vs. existing children changed.
				if (measuringExistingChildren)
				{
					if (pChildNode is null ||
						pChildNode.GetNext() is null)
					{
						// We were measuring existing children, in case 2 or 3, but ran out of content.
						// Switch to creating and measuring new nodes.
						measuringExistingChildren = false;
					}
				}
				else
				{
					if (childIndexInCollection == firstChildIndex)
					{
						// We were measuring new children, in case 3, and hit the overlap point. Switch to
						// the existing collection.
						measuringExistingChildren = true;
					}
				}
			}

		} while (pChildNode is not null &&
				 pBreak is null &&
				 !finishMeasureWithoutBreak);

		ClearChildren(pFirstChildToDelete);
		m_pFirstChild = pNewFirstChild;
		m_firstChildIndex = (pPreviousPageBreak is null) ? 0 : pPreviousPageBreak.BlockIndexInCollection;

		// Add padding to desired size.
		m_desiredSize.Width += (float)(padding.Left + padding.Right);
		m_desiredSize.Height += (float)(padding.Top + padding.Bottom);

		m_pBreak = pBreak;
		pBreak = null;

		// Remove any embedded elements that were added during formatting but then found to not fit on the page.
		RemoveClippedEmbeddedUIElements();
	}

	protected override void ArrangeCore(Size finalSize)
	{
		Thickness padding;
		Size contentFinalSize = default;

		// Adjust the final size and viewport to account for padding.
		// TODO Uno (integrate): BlockLayoutHelpers::GetPagePadding
		BlockLayoutHelpers.GetPagePadding(m_pPageOwner, out padding);
		contentFinalSize.Width = Math.Max(0.0f, (float)(finalSize.Width - (padding.Left + padding.Right)));
		contentFinalSize.Height = Math.Max(0.0f, (float)(finalSize.Height - (padding.Top + padding.Bottom)));

		// Since PageNode calls ContainerNode::ArrangeCore, that will invalidate any children nodes
		// if Arrange is not valid. PageNode-specific logic just resets embedded element visibility.
		// The Visibility is actually set in Render, when lines are drawn, but since Arrange guarantees that Render will
		// follow, this is okay.
		if (IsArrangeDirty())
		{
			// Reset visible state on all embedded elements since any visible ones will be re-arranged/re-rendered.
			for (int it = 0; it < m_embeddedElements.Count; ++it)
			{
				var info = m_embeddedElements[it];
				info.isVisible = false;
				m_embeddedElements[it] = info;
			}
		}

		// Arrange all children, then embedded elements. Embedded elements must be arranged after children because
		// offsets are determined when lines are drawn.
		base.ArrangeCore(contentFinalSize);

		ArrangeEmbeddedElements();

		// Add padding back to render size, but never exceed final height.  We bubble up the
		// real render width (which might exceed the final width) to get FrameworkElement to
		// apply a layout clip in scenarios where we were unable to fit within the constraint.
		// See comments in ParagraphNode::ArrangeCore for more details.
		m_renderSize.Width = (float)(m_renderSize.Width + padding.Left + padding.Right);
		m_renderSize.Height = (float)Math.Min(finalSize.Height, (m_renderSize.Height + padding.Top + padding.Bottom));
	}

	private BlockNode? GetChildNode(
		BlockCollection? pBlocks,
		uint childIndexInCollection)
	{
		BlockNode? pChildNode = null;
		Block? pBlock = null;

		if (pBlocks is not null &&
			childIndexInCollection < pBlocks.GetCount())
		{
			// DOCollection will AddRef pBlock when we access it. We don't want to hold a reference
			// to the block node here - BLE lifetime should be limited to a block's life time. Release the block
			// in Cleanup.
			// TODO Uno (integrate): CBlockCollection::GetItemWithAddRef -> indexer; GC handles lifetime.
			pBlock = (Block)pBlocks.GetItemWithAddRef(childIndexInCollection);
			// TODO Uno (integrate): OfTypeByIndex<KnownTypeIndex::Paragraph> -> "pBlock is Paragraph".
			if (pBlock is Paragraph)
			{
				pChildNode = new ParagraphNode(m_pBlockLayoutEngine, (Paragraph)pBlock, this);
			}
			else
			{
				MUX_ASSERT(false);
			}
		}
		else if (childIndexInCollection == 0 &&
				 (pBlocks is null || pBlocks.GetCount() == 0))
		{
			// Block collection may be NULL (TextBlock) or empty (contentless RichTextBlock).
			// For an empty block collection, one line of content should still be measured at layout owner's properties.
			pChildNode = new ParagraphNode(m_pBlockLayoutEngine, null, this);
		}

		return pChildNode;
	}

	//------------------------------------------------------------------------
	//
	//  PageNode::FindElement
	//
	//  Synopsis:
	//      Finds the EmbeddedElementInfo entry associated with the given embedded UIElement.
	//
	//------------------------------------------------------------------------
	private bool FindElement(
		InlineUIContainer pContainer,
		ParagraphNode pParagraphNode,
		out EmbeddedElementInfo pElementInfo,
		out uint pIndex)
	{
		uint size = (uint)m_embeddedElements.Count;

		for (uint i = 0; i < size; ++i)
		{
			EmbeddedElementInfo elementInfo = m_embeddedElements[(int)i];

			if (elementInfo.pContainer == pContainer)
			{
				// If we matched the element, assert that the paragraph container is the same.
				MUX_ASSERT(elementInfo.pParagraphNode == pParagraphNode);

				pElementInfo = elementInfo;
				pIndex = i;

				return true;
			}
		}

		pElementInfo = default;
		pIndex = 0;
		return false;
	}

	// Overload matching FindElement(pContainer, pParagraphNode) and
	// FindElement(pContainer, pParagraphNode, &elementInfo) C++ default-arg call sites.
	private bool FindElement(
		InlineUIContainer pContainer,
		ParagraphNode pParagraphNode,
		out EmbeddedElementInfo pElementInfo) => FindElement(pContainer, pParagraphNode, out pElementInfo, out _);

	private bool FindElement(
		InlineUIContainer pContainer,
		ParagraphNode pParagraphNode) => FindElement(pContainer, pParagraphNode, out _, out _);

	//------------------------------------------------------------------------
	//
	//  PageNode::ArrangeEmbeddedElements
	//
	//  Synopsis:
	//      Arranges embedded elements, if any.
	//
	//------------------------------------------------------------------------
	private void ArrangeEmbeddedElements()
	{
		InlineUIContainer? pContainer = null;
		UIElement? pElement = null;

		foreach (var info in m_embeddedElements)
		{
			pContainer = info.pContainer;

			// TODO Uno (integrate): CInlineUIContainer::GetChild
			pElement = pContainer.GetChild();
			Rect arrangeRect = new() { X = info.position.X, Y = info.position.Y, Width = 0, Height = 0 };

			// Elements that don't overlap with the viewport must be explicitly pushed outside.
			// Since, for performance reasons, we don't measure lines outside the viewport, we don't
			// know precisely where they render.  But any position that gets properly clipped is fine.
			if (!info.isVisible)
			{
				arrangeRect.Y = m_renderSize.Height;
			}

			// TODO Uno (integrate): CUIElement::HasLayoutStorage / GetLayoutStorage()->m_desiredSize -
			// use the element's measured desired size to size the arrange rect.
			if (pElement.HasLayoutStorage)
			{
				var pLayoutStorage = pElement.GetLayoutStorage();
				arrangeRect.Width = pLayoutStorage.m_desiredSize.Width;
				arrangeRect.Height = pLayoutStorage.m_desiredSize.Height;
			}

			// TODO Uno (integrate): CUIElement::Arrange(XRECTF)
			pElement.Arrange(arrangeRect);
			pElement = null;
		}
	}

	//------------------------------------------------------------------------
	//
	//  PageNode::ClearEmbeddedElements
	//
	//  Synopsis:
	//      Empties the embedded element list.
	//
	//------------------------------------------------------------------------
	private void ClearEmbeddedElements()
	{
		InlineUIContainer? pContainer = null;
		UIElement? pElement = null;

		foreach (var it in m_embeddedElements)
		{
			pContainer = it.pContainer;

			// TODO Uno (integrate): CInlineUIContainer::GetChild can fail; C++ swallows on failure (SUCCEEDED(hr)).
			pElement = pContainer.GetChild();

			if (pElement is not null)
			{
				// TODO Uno (integrate): CFrameworkElement::RemoveChild (VERIFYHR in C++)
				m_pPageOwner.RemoveChild(pElement);
			}
			// TODO Uno (integrate): CInlineUIContainer::ClearCachedHost
			pContainer.ClearCachedHost();
			pElement = null;
		}

		m_embeddedElements.Clear();
	}

	//------------------------------------------------------------------------
	//
	//  PageNode::RemoveClippedEmbeddedUIElements
	//
	//  Synopsis:
	//      Removes embedded elements, if any, positioned past the end of this
	//      node.
	//
	//  Notes:
	//      Embedded elements are added to the page during line formatting, but
	//      after a line is formatted it may be determined that the line (or part of
	//      it) doesn't fit on the page. Embedded elements added in content that
	//      doesn't fit need to be removed.
	//      This method is called at the *end* of measure, so content length and
	//      previous break (for start position) are guaranteed to be set to accurately
	//      determine which elements are outside the page bounds. It should not be
	//      called before Measure or when Measure is invalid.
	//
	//------------------------------------------------------------------------
	private void RemoveClippedEmbeddedUIElements()
	{
		bool found = false;
		uint offset = 0;
		BlockCollection pBlocks = (BlockCollection)m_pElement;

		while (m_embeddedElements.Count > 0)
		{
			EmbeddedElementInfo info = m_embeddedElements[m_embeddedElements.Count - 1];

			// Get the offset for the element's InlineUIContainer in the block collection. This can be used to determine
			// whether it lies on this page, since PageNode operates at block collection-level indices.
			// TODO Uno (integrate): CBlockCollection::GetElementEdgeOffset(container, ElementStart, out offset, out found)
			pBlocks.GetElementEdgeOffset(
				info.pContainer,
				ElementEdge.ElementStart,
				out offset,
				out found);

			// If the element lies on this page, all previous elements will too, so stop removal loop.
			if (offset < GetStartPosition() + GetContentLength())
			{
				break;
			}

			RemoveElement(info.pContainer, (uint)(m_embeddedElements.Count - 1));
		}
	}

	// Gets the absolute start position of this PageNode in the BlockCollection.
	public uint GetStartPosition() => ((m_pPreviousBreak is null) ? 0 : ((BlockNodeBreak)m_pPreviousBreak).BreakIndex);

	// Gets the block-collection index of this page's first child. Used by the overflow render
	// walk to map the page's first ParagraphNode back to the master's Blocks slice.
	public uint GetFirstChildIndex() => m_firstChildIndex;

	// Gets FrameworkElement that hosts this page.
	public FrameworkElement GetPageOwner() => m_pPageOwner;
}
