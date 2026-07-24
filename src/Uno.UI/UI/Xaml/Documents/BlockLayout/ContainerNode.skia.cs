// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ContainerNode.h, ContainerNode.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

//---------------------------------------------------------------------------
//
//  ContainerNode
//
//  Base class for block nodes whose content is a collection of other nodes.
//
//---------------------------------------------------------------------------
internal abstract class ContainerNode : BlockNode
{
	protected BlockNode? m_pFirstChild;

	public ContainerNode(
		BlockLayoutEngine pBlockLayoutEngine,
		DependencyObject? pElement,
		ContainerNode? pParentNode)
		: base(pBlockLayoutEngine, pElement, pParentNode)
	{
		m_pFirstChild = null;
	}

	// ~ContainerNode override - the C++ destructor calls ClearChildren(m_pFirstChild) and
	// deletes m_pDrawingContext. Under the GC the drawing context is collected, but
	// ClearChildren has observable side effects on sibling links, so keep it.
	// TODO Uno (integrate): wire ClearChildren(m_pFirstChild) into the lead's BlockNode
	// disposal/cleanup path (e.g. CleanupRealizations / IDisposable).

	//---------------------------------------------------------------------------
	//
	// ContainerNode::GetBaselineAlignmentOffset
	//
	// Synopsis:
	//      Gets baseline alignment offset of first child.
	//
	//---------------------------------------------------------------------------
	public override float GetBaselineAlignmentOffset()
	{
		float baselineAlignmentOffset = (float)GetContentRenderingOffset().Y;
		if (m_pFirstChild != null)
		{
			baselineAlignmentOffset += m_pFirstChild.GetBaselineAlignmentOffset();
		}
		return baselineAlignmentOffset;
	}

	public override BlockNode? GetFirstChild() => m_pFirstChild;

	//---------------------------------------------------------------------------
	//
	// ContainerNode::GetChildOffset
	//
	// Synopsis:
	//      Gets the offset of a child relative to the container's coordinate
	//      system.
	//
	//---------------------------------------------------------------------------
	public Point GetChildOffset(BlockNode pChild)
	{
		Point childOffset = GetContentRenderingOffset();
		BlockNode? pNode = m_pFirstChild;

		// Use RenderSize for offsetting, this type of calculation is only really valid post-Arrange.
		while (pNode != pChild)
		{
			childOffset.Y += pNode!.GetRenderSize().Height;
			pNode = pNode.GetNext();
		}

		return childOffset;
	}

	public override bool IsAtInsertionPosition(uint position)
	{
		// ContainerNode may not be the root of block layout, so it should assume
		// that offsets will be adjusted by callers/parents to be within its scope.
		MUX_ASSERT(position < m_length);

		GetChildContainingPosition(
			position,
			out BlockNode? pChild,
			out uint childStartPosition,
			out _);

		if (pChild != null)
		{
			position -= childStartPosition;
			return pChild.IsAtInsertionPosition(position);
		}
		else
		{
			return false;
		}
	}

	public override uint PixelPositionToTextPosition(
		Point pixel,
		out TextGravity gravity)
	{
		uint position = 0;
		gravity = TextGravity.LineForwardCharacterForward;
		Point adjustedPoint = pixel;

		GetChildContainingPoint(
			adjustedPoint,
			out BlockNode? pChild,
			out Point childStartPoint,
			out uint childStartPosition);

		if (pChild != null)
		{
			adjustedPoint.X -= childStartPoint.X;
			adjustedPoint.Y -= childStartPoint.Y;
			position = pChild.PixelPositionToTextPosition(
				adjustedPoint,
				out gravity);

			position += childStartPosition;
		}

		return position;
	}

	public override void GetTextBounds(
		uint start,
		uint length,
		List<TextBounds> pBounds)
	{
		uint remainingLength = length;
		uint index = 0;

		MUX_ASSERT(length > 0);
		MUX_ASSERT(start < m_length);
		MUX_ASSERT((start + length) <= m_length);

		// Start+Length is inclusive, so end index is start + length - 1.
		GetChildContainingPosition(start, out BlockNode? pBoundsStartChild, out uint childStartPosition, out Point childOffset);
		GetChildContainingPosition(start + length - 1, out BlockNode? pBoundsEndChild, out _, out _);

		if (pBoundsStartChild != null)
		{
			start -= childStartPosition;
			do
			{
				MUX_ASSERT(remainingLength > 0);
				MUX_ASSERT(pBoundsStartChild != null);
				length = Math.Min(remainingLength, (pBoundsStartChild!.GetContentLength() - start));
				index = (uint)pBounds.Count;

				// Get bounds from the child, then translate back into container's space.
				pBoundsStartChild.GetTextBounds(start, length, pBounds);
				for (uint i = index; i < pBounds.Count; i++)
				{
					// NOTE (Uno): TextBounds is an immutable record struct, so the C++ in-place
					// mutation of (*pBounds)[i].rect.X/Y becomes a recomputed Rect + `with`.
					pBounds[(int)i] = pBounds[(int)i] with
					{
						Rect = new Rect(
							pBounds[(int)i].Rect.X + childOffset.X,
							pBounds[(int)i].Rect.Y + childOffset.Y,
							pBounds[(int)i].Rect.Width,
							pBounds[(int)i].Rect.Height)
					};
				}

				// Adjust start, length and child offset.
				// Subsequent children will start at 0.
				remainingLength -= length;
				childOffset.Y += pBoundsStartChild.GetRenderSize().Height;
				start = 0;

				if (pBoundsStartChild == pBoundsEndChild)
				{
					break;
				}
				else
				{
					pBoundsStartChild = pBoundsStartChild.GetNext();
				}
			}
			while (true);
		}
	}

	//---------------------------------------------------------------------------
	//
	// ContainerNode::ArrangeCore
	//
	//---------------------------------------------------------------------------
	protected override void ArrangeCore(Size finalSize)
	{
		Size remainingSize = finalSize;

		m_renderSize.Width = finalSize.Width;
		m_renderSize.Height = 0.0f;

		// Drawing context is not needed at this stage by container node, but may be used by the owner post-Arrange to generate edges.
		// For ContainerNode it is a proxy to children's DrawingContexts, which may be created by children during their
		// Arrange phase and then will be required to generate rendering instructions.
		if (m_pDrawingContext == null)
		{
			m_pDrawingContext = new ContainerDrawingContext(this);
		}

		if (m_pFirstChild != null)
		{
			BlockNode? pChildNode = m_pFirstChild;

			do
			{
				pChildNode.Arrange(remainingSize);

				Size childRenderSize = pChildNode.GetRenderSize();
				remainingSize.Height = Math.Max(0.0f, remainingSize.Height - childRenderSize.Height);
				m_renderSize.Height += childRenderSize.Height;
				m_renderSize.Width = Math.Max(m_renderSize.Width, childRenderSize.Width);

				pChildNode = pChildNode.GetNext();
			}
			while (pChildNode != null);
		}

		// RenderSize is either the sum of children's sizes or the final size, whichever is smaller.
		// If all children won't fit in the available slot, the node won't render out of bounds.
		m_renderSize.Height = Math.Min(m_renderSize.Height, finalSize.Height);
	}

	//---------------------------------------------------------------------------
	//
	// ContainerNode::RenderCore
	//
	//---------------------------------------------------------------------------
	protected override void DrawCore(bool forceDraw)
	{
		BlockNode? pChildNode = m_pFirstChild;

		while (pChildNode != null)
		{
			pChildNode.Draw(forceDraw);
			pChildNode = pChildNode.GetNext();
		}
	}

	//---------------------------------------------------------------------------
	//
	// ContainerNode::ClearChildren
	//
	// Synopsis:
	//      Deletes all child nodes starting at the specified child.
	//
	//---------------------------------------------------------------------------
	protected void ClearChildren(BlockNode? pStart)
	{
		BlockNode? pChildNode;

		// Make sure the start's previous node is not pointing to deleted memory.
		if (pStart != null &&
			pStart.GetPrevious() != null)
		{
			pStart.GetPrevious()!.SetNext(null);
		}

		while (pStart != null)
		{
			pChildNode = pStart.GetNext();
			// NOTE (Uno): C++ `delete pStart` dropped under the GC.
			pStart = pChildNode;
		}
	}

	private void GetChildContainingPosition(
		uint position,
		out BlockNode? ppChild,
		out uint pChildStartPosition,
		out Point pChildStartPoint)
	{
		BlockNode? pChild = m_pFirstChild;
		Point childStartPoint = GetContentRenderingOffset();
		uint childStartPosition = 0;

		MUX_ASSERT(position < m_length);

		while (pChild != null)
		{
			if (position < pChild.GetContentLength())
			{
				break;
			}
			else
			{
				childStartPoint.Y += pChild.GetRenderSize().Height;
				childStartPosition += pChild.GetContentLength();
				position -= pChild.GetContentLength();
				pChild = pChild.GetNext();
			}
		}

		ppChild = pChild;
		pChildStartPosition = childStartPosition;
		pChildStartPoint = childStartPoint;
	}

	private void GetChildContainingPoint(
		Point point,
		out BlockNode? ppChild,
		out Point pChildStartPoint,
		out uint pChildStartPosition)
	{
		BlockNode? pChild = m_pFirstChild;
		Point childStartPoint = GetContentRenderingOffset();
		uint childStartPosition = 0;
		Point localPoint = new Point(point.X - childStartPoint.X, point.Y - childStartPoint.Y);

		// Pixel hit testing must always snap to some child.
		// The the point is outside the children collection, snap to the first
		// or last child based on y-coordinate.
		while (pChild != null)
		{
			float childRenderHeight = (float)pChild.GetRenderSize().Height;

			if (localPoint.Y < childRenderHeight ||
				pChild.GetNext() == null)
			{
				break;
			}
			else
			{
				childStartPoint.Y += childRenderHeight;
				childStartPosition += pChild.GetContentLength();
				localPoint.Y -= childRenderHeight;
				pChild = pChild.GetNext();
			}
		}

		ppChild = pChild;
		pChildStartPosition = childStartPosition;
		pChildStartPoint = childStartPoint;
	}
}
