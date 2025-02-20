using System;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Uno.UI.Xaml.Controls;

internal partial class RPGraph
{
	internal void ResolveConstraints(
		DependencyObject parent,
		Uno.UI.Xaml.Core.CoreServices core)
	{
		foreach (var node in m_nodes)
		{
			object value;

			node.GetLeftOfValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				GetNodeByValue(value, parent, core, out var leftOfNode);
				node.SetLeftOfConstraint(leftOfNode);
			}

			node.GetAboveValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				GetNodeByValue(value, parent, core, out var aboveNode);
				node.SetAboveConstraint(aboveNode);
			}

			node.GetRightOfValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				GetNodeByValue(value, parent, core, out var rightOfNode);
				node.SetRightOfConstraint(rightOfNode);
			}

			node.GetBelowValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				GetNodeByValue(value, parent, core, out var belowNode);
				node.SetBelowConstraint(belowNode);
			}

			node.GetAlignHorizontalCenterWithValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				GetNodeByValue(value, parent, core, out var alignHorizontalCenterWithNode);
				node.SetAlignHorizontalCenterWithConstraint(alignHorizontalCenterWithNode);
			}

			node.GetAlignVerticalCenterWithValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				GetNodeByValue(value, parent, core, out var alignVerticalCenterWithNode);
				node.SetAlignVerticalCenterWithConstraint(alignVerticalCenterWithNode);
			}

			node.GetAlignLeftWithValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				GetNodeByValue(value, parent, core, out var alignLeftWithNode);
				node.SetAlignLeftWithConstraint(alignLeftWithNode);
			}

			node.GetAlignTopWithValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				GetNodeByValue(value, parent, core, out var alignTopWithNode);
				node.SetAlignTopWithConstraint(alignTopWithNode);
			}

			node.GetAlignRightWithValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				GetNodeByValue(value, parent, core, out var alignRightWithNode);
				node.SetAlignRightWithConstraint(alignRightWithNode);
			}

			node.GetAlignBottomWithValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				GetNodeByValue(value, parent, core, out var alignBottomWithNode);
				node.SetAlignBottomWithConstraint(alignBottomWithNode);
			}

			node.GetAlignLeftWithPanelValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				node.SetAlignLeftWithPanelConstraint((bool)value);
			}

			node.GetAlignTopWithPanelValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				node.SetAlignTopWithPanelConstraint((bool)value);
			}

			node.GetAlignRightWithPanelValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				node.SetAlignRightWithPanelConstraint((bool)value);
			}

			node.GetAlignBottomWithPanelValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				node.SetAlignBottomWithPanelConstraint((bool)value);
			}

			node.GetAlignHorizontalCenterWithPanelValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				node.SetAlignHorizontalCenterWithPanelConstraint((bool)value);
			}

			node.GetAlignVerticalCenterWithPanelValue(out value);
			if (value != null && value != DependencyProperty.UnsetValue)
			{
				node.SetAlignVerticalCenterWithPanelConstraint((bool)value);
			}
		}
	}

	internal void MeasureNodes(Size availableSize)
	{
		foreach (RPNode node in m_nodes)
		{
			MeasureNode(node, availableSize);
		}

		m_availableSizeForNodeResolution = availableSize;
	}

	internal void ArrangeNodes(Rect finalRect)
	{
		var finalSize = new Size();
		finalSize.Height = finalRect.Height;
		finalSize.Width = finalRect.Width;

		// If the final size is the same as the available size that we used
		// to measure the nodes, this means that the pseudo-arrange pass  
		// that we did during the measure pass is, in fact, valid and the 
		// ArrangeRects that were calculated for each node are correct. In 
		// other words, we can just go ahead and call arrange on each
		// element. However, if the width and/or height of the final size
		// differs (e.g. when the element's HorizontalAlignment and/or
		// VerticalAlignment is something other than Stretch and thus the final
		// size corresponds to the desired size of the panel), we must first
		// recalculate the horizontal and/or vertical values of the ArrangeRects,
		// respectively.
		if (m_availableSizeForNodeResolution.Width != finalSize.Width)
		{
			foreach (RPNode node in m_nodes)
			{
				node.SetArrangedHorizontally(false);
			}

			foreach (RPNode node in m_nodes)
			{
				ArrangeNodeHorizontally(node, finalSize);
			}
		}

		if (m_availableSizeForNodeResolution.Height != finalSize.Height)
		{
			foreach (RPNode node in m_nodes)
			{
				node.SetArrangedVertically(false);
			}

			foreach (RPNode node in m_nodes)
			{
				ArrangeNodeVertically(node, finalSize);
			}
		}

		m_availableSizeForNodeResolution = finalSize;

		foreach (RPNode node in m_nodes)
		{
			MUX_ASSERT(node.IsArranged());

			Rect layoutSlot = new Rect();
			layoutSlot.X = Math.Max(node.m_arrangeRect.X + finalRect.X, 0.0f);
			layoutSlot.Y = Math.Max(node.m_arrangeRect.Y + finalRect.Y, 0.0f);
			layoutSlot.Width = Math.Max(node.m_arrangeRect.Width, 0.0f);
			layoutSlot.Height = Math.Max(node.m_arrangeRect.Height, 0.0f);

			node.Arrange(layoutSlot);
		}
	}

	internal Size CalculateDesiredSize()
	{
		Size maxDesiredSize = new Size(0.0f, 0.0f);

		MarkHorizontalAndVerticalLeaves();

		foreach (RPNode node in m_nodes)
		{
			if (node.m_isHorizontalLeaf)
			{
				m_minX = 0.0f;
				m_maxX = 0.0f;
				m_isMinCapped = false;
				m_isMaxCapped = false;

				AccumulatePositiveDesiredWidth(node, 0.0f);
				maxDesiredSize.Width = Math.Max(maxDesiredSize.Width, m_maxX - m_minX);
			}

			if (node.m_isVerticalLeaf)
			{
				m_minY = 0.0f;
				m_maxY = 0.0f;
				m_isMinCapped = false;
				m_isMaxCapped = false;

				AccumulatePositiveDesiredHeight(node, 0.0f);
				maxDesiredSize.Height = Math.Max(maxDesiredSize.Height, m_maxY - m_minY);
			}
		}

		return maxDesiredSize;
	}

	private void GetNodeByValue(
		object value,
		DependencyObject parent,
		Uno.UI.Xaml.Core.CoreServices core,
		out RPNode ppNode)
	{
		// Here we will have either a valueString which corresponds to the name
		// of the element we are looking for, or a valueObject of type UIElement
		// which is a direct reference to said element.
		if (value is string)
		{
			string name = null;
			if (value is string valueString)
			{
				name = valueString;
			}

			if (!string.IsNullOrEmpty(name))
			{
				foreach (RPNode node in m_nodes)
				{
					if (name.Equals(node.GetName()))
					{
						ppNode = node;
						return;
					}
				}

				// If there is no match within the children, the target might
				// actually be a deferred element. If that's the case, we will
				// create a node for this deferred element and inject it into 
				// the graph.
				// TODO Uno: We don't support a concept of deferred elements yet.
				//var deferredElement = core.GetDeferredElementIfExists(name, namescopeOwner, nameScopeType);

				//if (deferredElement && deferredElement.GetParent() == parent)
				//{
				//	*ppNode = &(*m_nodes.emplace_after(it, deferredElement));
				//	return S_OK;
				//}

				// If there is truly no matching node in the end, then we must 
				// throw an InvalidOperationException. We will fail fast here 
				// and let the CRelativePanel handle the rest.
				throw new InvalidOperationException("No matching node found");
			}
		}
		else
		{
			UIElement valueAsUIElement = null;
			if (value is UIElement)
			{
				valueAsUIElement = value as UIElement;
			}
			else if (value is ElementNameSubject elementNameSubject) // TODO Uno specific: We get ElementNameSubject instead of string here
			{
				valueAsUIElement = elementNameSubject.ElementInstance as UIElement;
			}

			if (valueAsUIElement != null)
			{
				foreach (RPNode node in m_nodes)
				{
					if (node.GetElement() == valueAsUIElement)
					{
						ppNode = node;
						return;
					}
				}

				// If there is no match, we must throw an InvalidOperationException.
				// We will fail fast here and let the CRelativePanel handle the rest.
				throw new InvalidOperationException("Node reference not found");
			}
		}

		ppNode = null;
		return;
	}

	private void CalculateMeasureRectHorizontally(RPNode node, Size availableSize, out double x, out double width)
	{
		bool isHorizontallyCenteredFromLeft = false;
		bool isHorizontallyCenteredFromRight = false;

		// The initial values correspond to the entire available space. In
		// other words, the edges of the element are aligned to the edges
		// of the panel by default. We will now rain each side of this
		// space as necessary.
		x = 0.0f;
		width = availableSize.Width;

		// If we have infinite available width, then the Width of the
		// MeasureRect is also infinite; we do not have to rain it.
		if (availableSize.Width != double.PositiveInfinity)
		{
			// Constrain the left side of the available space, i.e.
			// a) The child has its left edge aligned with the panel (default),
			// b) The child has its left edge aligned with the left edge of a sibling,
			// or c) The child is positioned to the right of a sibling.
			//
			//  |;;                 |               |                                                   
			//  |;;                 |               |                
			//  |;;                 |:::::::::::::::|                       ;;:::::::::::::;; 
			//  |;;                 |;             ;|       .               ;;             ;;
			//  |;;                 |;             ;|     .;;............   ;;             ;;
			//  |;;                 |;             ;|   .;;;;::::::::::::   ;;             ;;
			//  |;;                 |;             ;|    ':;;::::::::::::   ;;             ;;
			//  |;;                 |;             ;|      ':               ;;             ;;       
			//  |;;                 |:::::::::::::::|                       :::::::::::::::::
			//  |;;                 |               |               
			//  |;;                 |               |
			//  AlignLeftWithPanel  AlignLeftWith   RightOf
			//
			if (!node.IsAlignLeftWithPanel())
			{
				if (node.IsAlignLeftWith())
				{
					RPNode alignLeftWithNeighbor = node.m_alignLeftWithNode;
					double restrictedHorizontalSpace = alignLeftWithNeighbor.m_arrangeRect.X;

					x = restrictedHorizontalSpace;
					width -= restrictedHorizontalSpace;
				}
				else if (node.IsAlignHorizontalCenterWith())
				{
					isHorizontallyCenteredFromLeft = true;
				}
				else if (node.IsRightOf())
				{
					RPNode rightOfNeighbor = node.m_rightOfNode;
					double restrictedHorizontalSpace = rightOfNeighbor.m_arrangeRect.X + rightOfNeighbor.m_arrangeRect.Width;

					x = restrictedHorizontalSpace;
					width -= restrictedHorizontalSpace;
				}
			}

			// Constrain the right side of the available space, i.e.
			// a) The child has its right edge aligned with the panel (default),
			// b) The child has its right edge aligned with the right edge of a sibling,
			// or c) The child is positioned to the left of a sibling.
			//  
			//                                          |               |                   ;;|
			//                                          |               |                   ;;|
			//  ;;:::::::::::::;;                       |;:::::::::::::;|                   ;;|
			//  ;;             ;;               .       |;             ;|                   ;;|
			//  ;;             ;;   ............;;.     |;             ;|                   ;;|
			//  ;;             ;;   ::::::::::::;;;;.   |;             ;|                   ;;|
			//  ;;             ;;   ::::::::::::;;:'    |;             ;|                   ;;|
			//  ;;             ;;               :'      |;             ;|                   ;;|
			//  :::::::::::::::::                       |:::::::::::::::|                   ;;|
			//                                          |               |                   ;;|
			//                                          |               |                   ;;|
			//                                          LeftOf          AlignRightWith      AlignRightWithPanel
			//
			if (!node.IsAlignRightWithPanel())
			{
				if (node.IsAlignRightWith())
				{
					RPNode alignRightWithNeighbor = node.m_alignRightWithNode;

					width -= availableSize.Width - (alignRightWithNeighbor.m_arrangeRect.X + alignRightWithNeighbor.m_arrangeRect.Width);
				}
				else if (node.IsAlignHorizontalCenterWith())
				{
					isHorizontallyCenteredFromRight = true;
				}
				else if (node.IsLeftOf())
				{
					RPNode leftOfNeighbor = node.m_leftOfNode;

					width -= availableSize.Width - leftOfNeighbor.m_arrangeRect.X;
				}
			}

			if (isHorizontallyCenteredFromLeft && isHorizontallyCenteredFromRight)
			{
				RPNode alignHorizontalCenterWithNeighbor = node.m_alignHorizontalCenterWithNode;
				double centerOfNeighbor = alignHorizontalCenterWithNeighbor.m_arrangeRect.X + (alignHorizontalCenterWithNeighbor.m_arrangeRect.Width / 2.0f);
				width = Math.Min(centerOfNeighbor, availableSize.Width - centerOfNeighbor) * 2.0f;
				x = centerOfNeighbor - (width / 2.0f);
			}
		}
	}

	private void CalculateMeasureRectVertically(RPNode node, Size availableSize, out double y, out double height)
	{
		bool isVerticallyCenteredFromTop = false;
		bool isVerticallyCenteredFromBottom = false;

		// The initial values correspond to the entire available space. In
		// other words, the edges of the element are aligned to the edges
		// of the panel by default. We will now rain each side of this
		// space as necessary.
		y = 0.0f;
		height = availableSize.Height;

		// If we have infinite available height, then the Height of the
		// MeasureRect is also infinite; we do not have to rain it.
		if (availableSize.Height != double.PositiveInfinity)
		{
			// Constrain the top of the available space, i.e.
			// a) The child has its top edge aligned with the panel (default),
			// b) The child has its top edge aligned with the top edge of a sibling,
			// or c) The child is positioned to the below a sibling.
			//
			//  ================================== AlignTopWithPanel
			//  .................
			//
			//
			//
			//  --------;;=============;;--------- AlignTopWith
			//          ;;             ;;
			//          ;;             ;;
			//          ;;             ;;
			//          ;;             ;;
			//          ;;             ;;
			//  --------.=============.--------- Below 
			//                  .
			//                .:;:.
			//              .:;;;;;:.
			//                ;;;;;
			//                ;;;;;
			//                ;;;;;
			//                ;;;;;
			//                ;;;;;
			//
			//          ;;......:;; 
			//          ;;             ;;
			//          ;;             ;;
			//          ;;             ;;
			//          ;;             ;;
			//          ;;             ;;
			//          ........:
			//
			if (!node.IsAlignTopWithPanel())
			{
				if (node.IsAlignTopWith())
				{
					RPNode alignTopWithNeighbor = node.m_alignTopWithNode;
					double restrictedVerticalSpace = alignTopWithNeighbor.m_arrangeRect.Y;

					y = restrictedVerticalSpace;
					height -= restrictedVerticalSpace;
				}
				else if (node.IsAlignVerticalCenterWith())
				{
					isVerticallyCenteredFromTop = true;
				}
				else if (node.IsBelow())
				{
					RPNode belowNeighbor = node.m_belowNode;
					double restrictedVerticalSpace = belowNeighbor.m_arrangeRect.Y + belowNeighbor.m_arrangeRect.Height;

					y = restrictedVerticalSpace;
					height -= restrictedVerticalSpace;
				}
			}

			// Constrain the bottom of the available space, i.e.
			// a) The child has its bottom edge aligned with the panel (default),
			// b) The child has its bottom edge aligned with the bottom edge of a sibling,
			// or c) The child is positioned to the above a sibling.
			//
			//          ;;......:;; 
			//          ;;             ;;
			//          ;;             ;;
			//          ;;             ;;
			//          ;;             ;;
			//          ;;             ;;
			//          ........:
			//
			//                ;;;;;
			//                ;;;;;
			//                ;;;;;
			//                ;;;;;
			//                ;;;;;
			//              ..;;;;;..
			//               '..:'
			//                 ':`
			//                  
			//  --------;;=============;;--------- Above 
			//          ;;             ;;
			//          ;;             ;;
			//          ;;             ;;
			//          ;;             ;;
			//          ;;             ;;
			//  --------.=============.--------- AlignBottomWith
			//
			// 
			//
			//  .................
			//  ================================== AlignBottomWithPanel
			//
			if (!node.IsAlignBottomWithPanel())
			{
				if (node.IsAlignBottomWith())
				{
					RPNode alignBottomWithNeighbor = node.m_alignBottomWithNode;

					height -= availableSize.Height - (alignBottomWithNeighbor.m_arrangeRect.Y + alignBottomWithNeighbor.m_arrangeRect.Height);
				}
				else if (node.IsAlignVerticalCenterWith())
				{
					isVerticallyCenteredFromBottom = true;
				}
				else if (node.IsAbove())
				{
					RPNode aboveNeighbor = node.m_aboveNode;

					height -= availableSize.Height - aboveNeighbor.m_arrangeRect.Y;
				}
			}

			if (isVerticallyCenteredFromTop && isVerticallyCenteredFromBottom)
			{
				RPNode alignVerticalCenterWithNeighbor = node.m_alignVerticalCenterWithNode;
				double centerOfNeighbor = alignVerticalCenterWithNeighbor.m_arrangeRect.Y + (alignVerticalCenterWithNeighbor.m_arrangeRect.Height / 2.0f);
				height = Math.Min(centerOfNeighbor, availableSize.Height - centerOfNeighbor) * 2.0f;
				y = centerOfNeighbor - (height / 2.0f);
			}
		}
	}

	void CalculateArrangeRectHorizontally(RPNode node, out double x, out double width)
	{
		Rect measureRect = node.m_measureRect;
		double desiredWidth = Math.Min(measureRect.Width, node.GetDesiredWidth());

		MUX_ASSERT(node.IsMeasured() && (measureRect.Width != double.PositiveInfinity));

		// The initial values correspond to the left corner, using the 
		// desired size of element. If no attached properties were set, 
		// this means that the element will default to the left corner of
		// the panel.
		x = measureRect.X;
		width = desiredWidth;

		if (node.IsLeftAnchored())
		{
			if (node.IsRightAnchored())
			{
				x = measureRect.X;
				width = measureRect.Width;
			}
			else
			{
				x = measureRect.X;
				width = desiredWidth;
			}
		}
		else if (node.IsRightAnchored())
		{
			x = measureRect.X + measureRect.Width - desiredWidth;
			width = desiredWidth;
		}
		else if (node.IsHorizontalCenterAnchored())
		{
			x = measureRect.X + (measureRect.Width / 2.0f) - (desiredWidth / 2.0f);
			width = desiredWidth;
		}
	}

	void CalculateArrangeRectVertically(RPNode node, out double y, out double height)
	{
		Rect measureRect = node.m_measureRect;
		double desiredHeight = Math.Min(measureRect.Height, node.GetDesiredHeight());

		MUX_ASSERT(node.IsMeasured() && (measureRect.Height != double.PositiveInfinity));

		// The initial values correspond to the top corner, using the 
		// desired size of element. If no attached properties were set, 
		// this means that the element will default to the top corner of
		// the panel.
		y = measureRect.Y;
		height = desiredHeight;

		if (node.IsTopAnchored())
		{
			if (node.IsBottomAnchored())
			{
				y = measureRect.Y;
				height = measureRect.Height;
			}
			else
			{
				y = measureRect.Y;
				height = desiredHeight;
			}
		}
		else if (node.IsBottomAnchored())
		{
			y = measureRect.Y + measureRect.Height - desiredHeight;
			height = desiredHeight;
		}
		else if (node.IsVerticalCenterAnchored())
		{
			y = measureRect.Y + (measureRect.Height / 2.0f) - (desiredHeight / 2.0f);
			height = desiredHeight;
		}
	}

	void MarkHorizontalAndVerticalLeaves()
	{
		foreach (RPNode node in m_nodes)
		{
			node.m_isHorizontalLeaf = true;
			node.m_isVerticalLeaf = true;
		}

		foreach (RPNode node in m_nodes)
		{
			node.UnmarkNeighborsAsHorizontalOrVerticalLeaves();
		}
	}

	void AccumulatePositiveDesiredWidth(RPNode node, double x)
	{
		double initialX = x;
		bool isHorizontallyCenteredFromLeft = false;
		bool isHorizontallyCenteredFromRight = false;

		MUX_ASSERT(node.IsMeasured());

		// If we are going in the positive direction, move the cursor
		// right by the desired width of the node with which we are 
		// currently working and refresh the maximum positive value.
		x += node.GetDesiredWidth();
		m_maxX = Math.Max(m_maxX, x);

		if (node.IsAlignLeftWithPanel())
		{
			if (!m_isMaxCapped)
			{
				m_maxX = x;
				m_isMaxCapped = true;
			}
		}
		else if (node.IsAlignLeftWith())
		{
			// If the AlignLeftWithNode and AlignRightWithNode are the
			// same element, we can skip the former, since we will move 
			// through the latter later.
			if (node.m_alignLeftWithNode != node.m_alignRightWithNode)
			{
				AccumulateNegativeDesiredWidth(node.m_alignLeftWithNode, x);
			}
		}
		else if (node.IsAlignHorizontalCenterWith())
		{
			isHorizontallyCenteredFromLeft = true;
		}
		else if (node.IsRightOf())
		{
			AccumulatePositiveDesiredWidth(node.m_rightOfNode, x);
		}

		if (node.IsAlignRightWithPanel())
		{
			if (m_isMinCapped)
			{
				m_minX = Math.Min(m_minX, initialX);
			}
			else
			{
				m_minX = initialX;
				m_isMinCapped = true;
			}
		}
		else if (node.IsAlignRightWith())
		{
			// If this element's right is aligned to some other 
			// element's right, now we will be going in the positive
			// direction to that other element in order to continue the
			// traversal of the dependency chain. But first, since we 
			// arrived to the node where we currently are by going in
			// the positive direction, that means that we have already 
			// moved the cursor right to calculate the maximum positive 
			// value, so we will use the initial value of Y.
			AccumulatePositiveDesiredWidth(node.m_alignRightWithNode, initialX);
		}
		else if (node.IsAlignHorizontalCenterWith())
		{
			isHorizontallyCenteredFromRight = true;
		}
		else if (node.IsLeftOf())
		{
			// If this element is to the left of some other element,
			// now we will be going in the negative direction to that
			// other element in order to continue the traversal of the
			// dependency chain. But first, since we arrived to the
			// node where we currently are by going in the positive
			// direction, that means that we have already moved the 
			// cursor right to calculate the maximum positive value, so
			// we will use the initial value of X.
			AccumulateNegativeDesiredWidth(node.m_leftOfNode, initialX);
		}

		if (isHorizontallyCenteredFromLeft && isHorizontallyCenteredFromRight)
		{
			double centerX = x - (node.GetDesiredWidth() / 2.0f);
			double edgeX = centerX - (node.m_alignHorizontalCenterWithNode.GetDesiredWidth() / 2.0f);
			m_minX = Math.Min(m_minX, edgeX);
			AccumulatePositiveDesiredWidth(node.m_alignHorizontalCenterWithNode, edgeX);
		}
		else if (node.IsHorizontalCenterAnchored())
		{
			// If this node is horizontally anchored to the center, then it
			// means that it is the root of this dependency chain based on
			// the current definition of precedence for raints: 
			// e.g. AlignLeftWithPanel 
			// > AlignLeftWith 
			// > RightOf
			// > AlignHorizontalCenterWithPanel    
			// Thus, we can report its width as twice the width of 
			// either the difference from center to left or the difference
			// from center to right, whichever is the greatest.
			double centerX = x - (node.GetDesiredWidth() / 2.0f);
			double upper = m_maxX - centerX;
			double lower = centerX - m_minX;
			m_maxX = Math.Max(upper, lower) * 2.0f;
			m_minX = 0.0f;
		}
	}

	private void AccumulateNegativeDesiredWidth(RPNode node, double x)
	{
		double initialX = x;
		bool isHorizontallyCenteredFromLeft = false;
		bool isHorizontallyCenteredFromRight = false;

		MUX_ASSERT(node.IsMeasured());

		// If we are going in the negative direction, move the cursor
		// left by the desired width of the node with which we are 
		// currently working and refresh the minimum negative value.
		x -= node.GetDesiredWidth();
		m_minX = Math.Min(m_minX, x);

		if (node.IsAlignRightWithPanel())
		{
			if (!m_isMinCapped)
			{
				m_minX = x;
				m_isMinCapped = true;
			}
		}
		else if (node.IsAlignRightWith())
		{
			// If the AlignRightWithNode and AlignLeftWithNode are the
			// same element, we can skip the former, since we will move 
			// through the latter later.
			if (node.m_alignRightWithNode != node.m_alignLeftWithNode)
			{
				AccumulatePositiveDesiredWidth(node.m_alignRightWithNode, x);
			}
		}
		else if (node.IsAlignHorizontalCenterWith())
		{
			isHorizontallyCenteredFromRight = true;
		}
		else if (node.IsLeftOf())
		{
			AccumulateNegativeDesiredWidth(node.m_leftOfNode, x);
		}

		if (node.IsAlignLeftWithPanel())
		{
			if (m_isMaxCapped)
			{
				m_maxX = Math.Max(m_maxX, initialX);
			}
			else
			{
				m_maxX = initialX;
				m_isMaxCapped = true;
			}
		}
		else if (node.IsAlignLeftWith())
		{
			// If this element's left is aligned to some other element's
			// left, now we will be going in the negative direction to 
			// that other element in order to continue the traversal of
			// the dependency chain. But first, since we arrived to the
			// node where we currently are by going in the negative 
			// direction, that means that we have already moved the 
			// cursor left to calculate the minimum negative value,
			// so we will use the initial value of X.
			AccumulateNegativeDesiredWidth(node.m_alignLeftWithNode, initialX);
		}
		else if (node.IsAlignHorizontalCenterWith())
		{
			isHorizontallyCenteredFromLeft = true;
		}
		else if (node.IsRightOf())
		{
			// If this element is to the right of some other element,
			// now we will be going in the positive direction to that
			// other element in order to continue the traversal of the
			// dependency chain. But first, since we arrived to the
			// node where we currently are by going in the negative
			// direction, that means that we have already moved the 
			// cursor left to calculate the minimum negative value, so
			// we will use the initial value of X.
			AccumulatePositiveDesiredWidth(node.m_rightOfNode, initialX);
		}

		if (isHorizontallyCenteredFromLeft && isHorizontallyCenteredFromRight)
		{
			double centerX = x + (node.GetDesiredWidth() / 2.0f);
			double edgeX = centerX + (node.m_alignHorizontalCenterWithNode.GetDesiredWidth() / 2.0f);
			m_maxX = Math.Max(m_maxX, edgeX);
			AccumulateNegativeDesiredWidth(node.m_alignHorizontalCenterWithNode, edgeX);
		}
		else if (node.IsHorizontalCenterAnchored())
		{
			// If this node is horizontally anchored to the center, then it
			// means that it is the root of this dependency chain based on
			// the current definition of precedence for raints: 
			// e.g. AlignLeftWithPanel 
			// > AlignLeftWith 
			// > RightOf
			// > AlignHorizontalCenterWithPanel    
			// Thus, we can report its width as twice the width of 
			// either the difference from center to left or the difference
			// from center to right, whichever is the greatest.
			double centerX = x + (node.GetDesiredWidth() / 2.0f);
			double upper = m_maxX - centerX;
			double lower = centerX - m_minX;
			m_maxX = Math.Max(upper, lower) * 2.0f;
			m_minX = 0.0f;
		}
	}

	void AccumulatePositiveDesiredHeight(RPNode node, double y)
	{
		double initialY = y;
		bool isVerticallyCenteredFromTop = false;
		bool isVerticallyCenteredFromBottom = false;

		MUX_ASSERT(node.IsMeasured());

		// If we are going in the positive direction, move the cursor
		// up by the desired height of the node with which we are 
		// currently working and refresh the maximum positive value.
		y += node.GetDesiredHeight();
		m_maxY = Math.Max(m_maxY, y);

		if (node.IsAlignTopWithPanel())
		{
			if (!m_isMaxCapped)
			{
				m_maxY = y;
				m_isMaxCapped = true;
			}
		}
		else if (node.IsAlignTopWith())
		{
			// If the AlignTopWithNode and AlignBottomWithNode are the
			// same element, we can skip the former, since we will move 
			// through the latter later.
			if (node.m_alignTopWithNode != node.m_alignBottomWithNode)
			{
				AccumulateNegativeDesiredHeight(node.m_alignTopWithNode, y);
			}
		}
		else if (node.IsAlignVerticalCenterWith())
		{
			isVerticallyCenteredFromTop = true;
		}
		else if (node.IsBelow())
		{
			AccumulatePositiveDesiredHeight(node.m_belowNode, y);
		}

		if (node.IsAlignBottomWithPanel())
		{
			if (m_isMinCapped)
			{
				m_minY = Math.Min(m_minY, initialY);
			}
			else
			{
				m_minY = initialY;
				m_isMinCapped = true;
			}
		}
		else if (node.IsAlignBottomWith())
		{
			// If this element's bottom is aligned to some other 
			// element's bottom, now we will be going in the positive
			// direction to that other element in order to continue the
			// traversal of the dependency chain. But first, since we 
			// arrived to the node where we currently are by going in
			// the positive direction, that means that we have already 
			// moved the cursor up to calculate the maximum positive 
			// value, so we will use the initial value of Y.
			AccumulatePositiveDesiredHeight(node.m_alignBottomWithNode, initialY);
		}
		else if (node.IsAlignVerticalCenterWith())
		{
			isVerticallyCenteredFromBottom = true;
		}
		else if (node.IsAbove())
		{
			// If this element is above some other element, now we will 
			// be going in the negative direction to that other element
			// in order to continue the traversal of the dependency  
			// chain. But first, since we arrived to the node where we 
			// currently are by going in the positive direction, that
			// means that we have already moved the cursor up to 
			// calculate the maximum positive value, so we will use
			// the initial value of Y.
			AccumulateNegativeDesiredHeight(node.m_aboveNode, initialY);
		}

		if (isVerticallyCenteredFromTop && isVerticallyCenteredFromBottom)
		{
			double centerY = y - (node.GetDesiredHeight() / 2.0f);
			double edgeY = centerY - (node.m_alignVerticalCenterWithNode.GetDesiredHeight() / 2.0f);
			m_minY = Math.Min(m_minY, edgeY);
			AccumulatePositiveDesiredHeight(node.m_alignVerticalCenterWithNode, edgeY);
		}
		else if (node.IsVerticalCenterAnchored())
		{
			// If this node is vertically anchored to the center, then it
			// means that it is the root of this dependency chain based on
			// the current definition of precedence for raints: 
			// e.g. AlignTopWithPanel 
			// > AlignTopWith
			// > Below
			// > AlignVerticalCenterWithPanel 
			// Thus, we can report its height as twice the height of 
			// either the difference from center to top or the difference
			// from center to bottom, whichever is the greatest.
			double centerY = y - (node.GetDesiredHeight() / 2.0f);
			double upper = m_maxY - centerY;
			double lower = centerY - m_minY;
			m_maxY = Math.Max(upper, lower) * 2.0f;
			m_minY = 0.0f;
		}
	}

	void AccumulateNegativeDesiredHeight(RPNode node, double y)
	{
		double initialY = y;
		bool isVerticallyCenteredFromTop = false;
		bool isVerticallyCenteredFromBottom = false;

		MUX_ASSERT(node.IsMeasured());

		// If we are going in the negative direction, move the cursor
		// down by the desired height of the node with which we are 
		// currently working and refresh the minimum negative value.
		y -= node.GetDesiredHeight();
		m_minY = Math.Min(m_minY, y);

		if (node.IsAlignBottomWithPanel())
		{
			if (!m_isMinCapped)
			{
				m_minY = y;
				m_isMinCapped = true;
			}
		}
		else if (node.IsAlignBottomWith())
		{
			// If the AlignBottomWithNode and AlignTopWithNode are the
			// same element, we can skip the former, since we will move 
			// through the latter later.
			if (node.m_alignBottomWithNode != node.m_alignTopWithNode)
			{
				AccumulatePositiveDesiredHeight(node.m_alignBottomWithNode, y);
			}
		}
		else if (node.IsAlignVerticalCenterWith())
		{
			isVerticallyCenteredFromBottom = true;
		}
		else if (node.IsAbove())
		{
			AccumulateNegativeDesiredHeight(node.m_aboveNode, y);
		}

		if (node.IsAlignTopWithPanel())
		{
			if (m_isMaxCapped)
			{
				m_maxY = Math.Max(m_maxY, initialY);
			}
			else
			{
				m_maxY = initialY;
				m_isMaxCapped = true;
			}
		}
		else if (node.IsAlignTopWith())
		{
			// If this element's top is aligned to some other element's
			// top, now we will be going in the negative direction to 
			// that other element in order to continue the traversal of
			// the dependency chain. But first, since we arrived to the
			// node where we currently are by going in the negative 
			// direction, that means that we have already moved the 
			// cursor down to calculate the minimum negative value,
			// so we will use the initial value of Y.
			AccumulateNegativeDesiredHeight(node.m_alignTopWithNode, initialY);
		}
		else if (node.IsAlignVerticalCenterWith())
		{
			isVerticallyCenteredFromTop = true;
		}
		else if (node.IsBelow())
		{
			// If this element is below some other element, now we'll
			// be going in the positive direction to that other element  
			// in order to continue the traversal of the dependency
			// chain. But first, since we arrived to the node where we
			// currently are by going in the negative direction, that
			// means that we have already moved the cursor down to
			// calculate the minimum negative value, so we will use
			// the initial value of Y.
			AccumulatePositiveDesiredHeight(node.m_belowNode, initialY);
		}

		if (isVerticallyCenteredFromTop && isVerticallyCenteredFromBottom)
		{
			double centerY = y + (node.GetDesiredHeight() / 2.0f);
			double edgeY = centerY + (node.m_alignVerticalCenterWithNode.GetDesiredHeight() / 2.0f);
			m_maxY = Math.Max(m_maxY, edgeY);
			AccumulateNegativeDesiredHeight(node.m_alignVerticalCenterWithNode, edgeY);
		}
		else if (node.IsVerticalCenterAnchored())
		{
			// If this node is vertically anchored to the center, then it
			// means that it is the root of this dependency chain based on
			// the current definition of precedence for raints: 
			// e.g. AlignTopWithPanel 
			// > AlignTopWith
			// > Below
			// > AlignVerticalCenterWithPanel 
			// Thus, we can report its height as twice the height of 
			// either the difference from center to top or the difference
			// from center to bottom, whichever is the greatest.
			double centerY = y + (node.GetDesiredHeight() / 2.0f);
			double upper = m_maxY - centerY;
			double lower = centerY - m_minY;
			m_maxY = Math.Max(upper, lower) * 2.0f;
			m_minY = 0.0f;
		}
	}

	private void MeasureNode(RPNode node, Size availableSize)
	{
		if (node == null)
		{
			return;
		}

		if (node.IsPending())
		{
			// If the node is already in the process of being resolved
			// but we tried to resolve it again, that means we are in the
			// middle of circular dependency and we must throw an 
			// InvalidOperationException. We will fail fast here and let
			// the CRelativePanel handle the rest.
			throw new InvalidOperationException("Circular dependency found in RelativePanel");
		}
		else if (node.IsUnresolved())
		{
			Size constrainedAvailableSize = new Size();

			// We must resolve the dependencies of this node first.
			// In the meantime, we will mark the state as pending.
			node.SetPending(true);

			MeasureNode(node.m_leftOfNode, availableSize);
			MeasureNode(node.m_aboveNode, availableSize);
			MeasureNode(node.m_rightOfNode, availableSize);
			MeasureNode(node.m_belowNode, availableSize);
			MeasureNode(node.m_alignLeftWithNode, availableSize);
			MeasureNode(node.m_alignTopWithNode, availableSize);
			MeasureNode(node.m_alignRightWithNode, availableSize);
			MeasureNode(node.m_alignBottomWithNode, availableSize);
			MeasureNode(node.m_alignHorizontalCenterWithNode, availableSize);
			MeasureNode(node.m_alignVerticalCenterWithNode, availableSize);

			node.SetPending(false);

			double nodeMeasureRectX, nodeMeasureRectWidth, nodeMeasureRectY, nodeMeasureRectHeight;
			CalculateMeasureRectHorizontally(node, availableSize, out nodeMeasureRectX, out nodeMeasureRectWidth);
			CalculateMeasureRectVertically(node, availableSize, out nodeMeasureRectY, out nodeMeasureRectHeight);
			node.m_measureRect.X = nodeMeasureRectX;
			node.m_measureRect.Y = nodeMeasureRectY;
			node.m_measureRect.Width = nodeMeasureRectWidth;
			node.m_measureRect.Height = nodeMeasureRectHeight;

			constrainedAvailableSize.Width = Math.Max(node.m_measureRect.Width, 0.0f);
			constrainedAvailableSize.Height = Math.Max(node.m_measureRect.Height, 0.0f);
			node.Measure(constrainedAvailableSize);
			node.SetMeasured(true);

			// (Pseudo-) Arranging against infinity does not make sense, so 
			// we will skip the calculations of the ArrangeRects if 
			// necessary. During the true arrange pass, we will be given a
			// non-infinite final size; we will do the necessary
			// calculations until then.
			if (availableSize.Width != double.PositiveInfinity)
			{
				double nodeArrangeRectX, nodeArrangeRectWidth;
				CalculateArrangeRectHorizontally(node, out nodeArrangeRectX, out nodeArrangeRectWidth);
				node.m_arrangeRect.X = nodeArrangeRectX;
				node.m_arrangeRect.Width = nodeArrangeRectWidth;
				node.SetArrangedHorizontally(true);
			}

			if (availableSize.Height != double.PositiveInfinity)
			{
				double nodeArrangeRectY, nodeArrangeRectHeight;
				CalculateArrangeRectVertically(node, out nodeArrangeRectY, out nodeArrangeRectHeight);
				node.m_arrangeRect.Y = nodeArrangeRectY;
				node.m_arrangeRect.Height = nodeArrangeRectHeight;
				node.SetArrangedVertically(true);
			}
		}
	}

	private void ArrangeNodeHorizontally(RPNode node, Size finalSize)
	{
		if (node == null)
		{
			return;
		}

		if (!node.IsArrangedHorizontally())
		{
			// We must resolve dependencies first.
			ArrangeNodeHorizontally(node.m_leftOfNode, finalSize);
			ArrangeNodeHorizontally(node.m_aboveNode, finalSize);
			ArrangeNodeHorizontally(node.m_rightOfNode, finalSize);
			ArrangeNodeHorizontally(node.m_belowNode, finalSize);
			ArrangeNodeHorizontally(node.m_alignLeftWithNode, finalSize);
			ArrangeNodeHorizontally(node.m_alignTopWithNode, finalSize);
			ArrangeNodeHorizontally(node.m_alignRightWithNode, finalSize);
			ArrangeNodeHorizontally(node.m_alignBottomWithNode, finalSize);
			ArrangeNodeHorizontally(node.m_alignHorizontalCenterWithNode, finalSize);
			ArrangeNodeHorizontally(node.m_alignVerticalCenterWithNode, finalSize);

			double nodeMeasureRectX, nodeMeasureRectWidth;
			CalculateMeasureRectHorizontally(node, finalSize, out nodeMeasureRectX, out nodeMeasureRectWidth);
			node.m_measureRect.X = nodeMeasureRectX;
			node.m_measureRect.Width = nodeMeasureRectWidth;
			double nodeArrangeRectX, nodeArrangeRectWidth;
			CalculateArrangeRectHorizontally(node, out nodeArrangeRectX, out nodeArrangeRectWidth);
			node.m_arrangeRect.X = nodeArrangeRectX;
			node.m_arrangeRect.Width = nodeArrangeRectWidth;
			node.SetArrangedHorizontally(true);
		}
	}

	private void ArrangeNodeVertically(RPNode node, Size finalSize)
	{
		if (node == null)
		{
			return;
		}

		if (!node.IsArrangedVertically())
		{
			// We must resolve dependencies first.
			ArrangeNodeVertically(node.m_leftOfNode, finalSize);
			ArrangeNodeVertically(node.m_aboveNode, finalSize);
			ArrangeNodeVertically(node.m_rightOfNode, finalSize);
			ArrangeNodeVertically(node.m_belowNode, finalSize);
			ArrangeNodeVertically(node.m_alignLeftWithNode, finalSize);
			ArrangeNodeVertically(node.m_alignTopWithNode, finalSize);
			ArrangeNodeVertically(node.m_alignRightWithNode, finalSize);
			ArrangeNodeVertically(node.m_alignBottomWithNode, finalSize);
			ArrangeNodeVertically(node.m_alignHorizontalCenterWithNode, finalSize);
			ArrangeNodeVertically(node.m_alignVerticalCenterWithNode, finalSize);

			double nodeMeasureRectY, nodeMeasureRectHeight;
			CalculateMeasureRectVertically(node, finalSize, out nodeMeasureRectY, out nodeMeasureRectHeight);
			node.m_measureRect.Y = nodeMeasureRectY;
			node.m_measureRect.Height = nodeMeasureRectHeight;
			double nodeArrangeRectY, nodeArrangeRectHeight;
			CalculateArrangeRectVertically(node, out nodeArrangeRectY, out nodeArrangeRectHeight);
			node.m_arrangeRect.Y = nodeArrangeRectY;
			node.m_arrangeRect.Height = nodeArrangeRectHeight;
			node.SetArrangedVertically(true);
		}
	}
}
