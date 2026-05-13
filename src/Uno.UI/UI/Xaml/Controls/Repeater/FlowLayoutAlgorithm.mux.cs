// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlowLayoutAlgorithm.cpp, commit 4b206bce3

using System;
using System.Collections.Specialized;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class FlowLayoutAlgorithm
{
	internal void InitializeForContext(
		VirtualizingLayoutContext context,
		IFlowLayoutAlgorithmDelegates callbacks)
	{
		m_algorithmCallbacks = callbacks;
		m_context = context;
		m_elementManager.SetContext(context);
	}

	internal void UninitializeForContext(VirtualizingLayoutContext context)
	{
		if (IsVirtualizingContext())
		{
			// This layout is about to be detached. Let go of all elements
			// being held and remove the layout state from the context.
			m_elementManager.ClearRealizedRange();
		}

		context.LayoutStateCore = null;
	}

	internal Size Measure(
		Size availableSize,
		VirtualizingLayoutContext context,
		bool isWrapping,
		double minItemSpacing,
		double lineSpacing,
		uint maxItemsPerLine,
		ScrollOrientation orientation,
		bool isVirtualizationEnabled,
		string layoutId)
	{
		SetScrollOrientation(orientation);

		// If minor size is infinity, there is only one line and no need to align that line.
		m_scrollOrientationSameAsFlow = Minor(availableSize) == double.PositiveInfinity;
		var realizationRect = RealizationRect();

		REPEATER_TRACE_INFO("%*s: \tMeasureLayout Realization(%.0f,%.0f,%.0f,%.0f)\n",
			context.Indent,
			layoutId,
			realizationRect.X, realizationRect.Y, realizationRect.Width, realizationRect.Height);

		var suggestedAnchorIndex = m_context.RecommendedAnchorIndex;
		if (m_elementManager.IsIndexValidInData(suggestedAnchorIndex))
		{
			var anchorRealized = m_elementManager.IsDataIndexRealized(suggestedAnchorIndex);
			if (!anchorRealized)
			{
				MakeAnchor(m_context, suggestedAnchorIndex, availableSize);
			}
		}

		if (isVirtualizationEnabled)
		{
			m_elementManager.OnBeginMeasure(orientation);
		}

		int anchorIndex = GetAnchorIndex(availableSize, isWrapping, minItemSpacing, isVirtualizationEnabled, layoutId);
		Generate(GenerateDirection.Forward, anchorIndex, availableSize, isWrapping, minItemSpacing, lineSpacing, maxItemsPerLine, isVirtualizationEnabled, layoutId);
		Generate(GenerateDirection.Backward, anchorIndex, availableSize, isWrapping, minItemSpacing, lineSpacing, maxItemsPerLine, isVirtualizationEnabled, layoutId);
		if (isWrapping && IsReflowRequired())
		{
			REPEATER_TRACE_INFO("%*s: \tReflow Pass \n", context.Indent, layoutId);
			var firstElementBounds = m_elementManager.GetLayoutBoundsForRealizedIndex(0);
			SetMinorStart(ref firstElementBounds, 0);
			m_elementManager.SetLayoutBoundsForRealizedIndex(0, firstElementBounds);
			Generate(GenerateDirection.Forward, 0 /*anchorIndex*/, availableSize, isWrapping, minItemSpacing, lineSpacing, maxItemsPerLine, isVirtualizationEnabled, layoutId);
		}

		RaiseLineArranged();
		m_collectionChangePending = false;
		var lastExtent = EstimateExtent(availableSize, layoutId);

		// When m_layoutRoundFactor was set, the layout origin and extent are rounded based on
		// that factor to avoid accumulations of double-to-float rounding imprecisions.
		m_lastExtent = m_layoutRoundFactor > 0.0 ? LayoutRound(lastExtent) : lastExtent;
		SetLayoutOrigin();

		return new Size(m_lastExtent.Width, m_lastExtent.Height);
	}

	internal Size Arrange(
		Size finalSize,
		VirtualizingLayoutContext context,
		bool isWrapping,
		FlowLayoutAlgorithm.LineAlignment lineAlignment,
		string layoutId)
	{
		REPEATER_TRACE_INFO("%*s: \tArrangeLayout \n", context.Indent, layoutId);

		ArrangeVirtualizingLayout(finalSize, lineAlignment, isWrapping, layoutId);

		return new Size(
			Math.Max(finalSize.Width, m_lastExtent.Width),
			Math.Max(finalSize.Height, m_lastExtent.Height));
	}

	private void MakeAnchor(
		VirtualizingLayoutContext context,
		int index,
		Size availableSize)
	{
		m_elementManager.ClearRealizedRange();
		// FlowLayout requires that the anchor is the first element in the row.
		var internalAnchor = m_algorithmCallbacks.Algorithm_GetAnchorForTargetElement(index, availableSize, context);
		MUX_ASSERT(internalAnchor.Index <= index);

		// No need to set the position of the anchor.
		// (0,0) is fine for now since the extent can
		// grow in any direction.
		for (int dataIndex = internalAnchor.Index; dataIndex < index + 1; ++dataIndex)
		{
			var element = context.GetOrCreateElementAt(dataIndex, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
			element.Measure(m_algorithmCallbacks.Algorithm_GetMeasureSize(dataIndex, availableSize, context));
			m_elementManager.Add(element, dataIndex);
		}
	}

	internal void OnItemsSourceChanged(
		object source,
		NotifyCollectionChangedEventArgs args,
		VirtualizingLayoutContext context)
	{
		m_elementManager.DataSourceChanged(source, args);
		m_collectionChangePending = true;
	}

	internal Size MeasureElement(
		UIElement element,
		int index,
		Size availableSize,
		VirtualizingLayoutContext context)
	{
		// Using the handy 'element' to determine the global scale factor because accessing the ItemsRepeater panel at this FlowLayoutAlgorithm level
		// would require significant additions. This scale factor is used to round the layout's origin and extent.
		EvaluateLayoutRoundFactor(context, element);

		var measureSize = m_algorithmCallbacks.Algorithm_GetMeasureSize(index, availableSize, context);
		element.Measure(measureSize);
		var desiredSize = element.DesiredSize;
		var provisionalArrangeSize = m_algorithmCallbacks.Algorithm_GetProvisionalArrangeSize(index, measureSize, desiredSize, context);

		m_algorithmCallbacks.Algorithm_OnElementMeasured(element, index, availableSize, measureSize, desiredSize, provisionalArrangeSize, context);

		return provisionalArrangeSize;
	}

	// #pragma region Measure related private methods

	private int GetAnchorIndex(
		Size availableSize,
		bool isWrapping,
		double minItemSpacing,
		bool isVirtualizationEnabled,
		string layoutId)
	{
		int anchorIndex = -1;
		Point anchorPosition = default;
		var context = m_context;

		if (!IsVirtualizingContext() || !isVirtualizationEnabled)
		{
			// Non virtualizing host, start generating from the element 0
			if (context.ItemCount > 0)
			{
				anchorIndex = 0;
			}
			else
			{
				anchorPosition = new Point(-1.0, -1.0);
			}
		}
		else
		{
			bool isRealizationWindowConnected = m_elementManager.IsWindowConnected(RealizationRect(), GetScrollOrientation(), m_scrollOrientationSameAsFlow);

			REPEATER_TRACE_INFO("%*s: \t%s\n",
				context.Indent,
				layoutId,
				isRealizationWindowConnected ? "Connected RealizationWindow." : "Disconnected RealizationWindow.");

			// Item spacing and size in non-virtualizing direction change can cause elements to reflow
			// and get a new column position. In that case we need the anchor to be positioned in the
			// correct column.
			bool needAnchorColumnReevaluation = isWrapping && (
				Minor(m_lastAvailableSize) != Minor(availableSize) ||
				m_lastItemSpacing != minItemSpacing ||
				m_collectionChangePending);

			var suggestedAnchorIndex = m_context.RecommendedAnchorIndex;

			bool isAnchorSuggestionValid = suggestedAnchorIndex >= 0 &&
				m_elementManager.IsDataIndexRealized(suggestedAnchorIndex);

			if (isAnchorSuggestionValid)
			{
				REPEATER_TRACE_INFO("%*s: \tUsing suggested anchor %d\n", context.Indent, layoutId, suggestedAnchorIndex);

				anchorIndex = m_algorithmCallbacks.Algorithm_GetAnchorForTargetElement(
					suggestedAnchorIndex,
					availableSize,
					context).Index;

				if (m_elementManager.IsDataIndexRealized(anchorIndex))
				{
					var anchorBounds = m_elementManager.GetLayoutBoundsForDataIndex(anchorIndex);
					if (needAnchorColumnReevaluation)
					{
						// We were provided a valid anchor, but its position might be incorrect because for example it is in
						// the wrong column. We do know that the anchor is the first element in the row, so we can force the minor position
						// to start at 0.
						anchorPosition = MinorMajorPoint(0, (float)MajorStart(anchorBounds));
					}
					else
					{
						anchorPosition = new Point(anchorBounds.X, anchorBounds.Y);
					}
				}
				else
				{
					// It is possible to end up in a situation during a collection change where GetAnchorForTargetElement returns an index
					// which is not in the realized range. Eg. insert one item at index 0 for a grid layout.
					// SuggestedAnchor will be 1 (used to be 0) and GetAnchorForTargetElement will return 0 (left most item in row). However 0 is not in the
					// realized range yet. In this case we realize the gap between the target anchor and the suggested anchor.
					int firstRealizedDataIndex = m_elementManager.GetDataIndexFromRealizedRangeIndex(0);
					MUX_ASSERT(anchorIndex < firstRealizedDataIndex);
					for (int i = firstRealizedDataIndex - 1; i >= anchorIndex; --i)
					{
						m_elementManager.EnsureElementRealized(false /*forward*/, i, layoutId);
					}

					var anchorBounds = m_elementManager.GetLayoutBoundsForDataIndex(suggestedAnchorIndex);
					anchorPosition = MinorMajorPoint(0, (float)MajorStart(anchorBounds));
				}
			}
			else if (needAnchorColumnReevaluation || !isRealizationWindowConnected)
			{
				if (needAnchorColumnReevaluation)
				{
					REPEATER_TRACE_INFO("%*s: \tNeedAnchorColumnReevaluation.\n", context.Indent, layoutId);
				}

				// The anchor is based on the realization window because a connected ItemsRepeater might intersect the realization window
				// but not the visible window. In that situation, we still need to produce a valid anchor.
				var anchorInfo = m_algorithmCallbacks.Algorithm_GetAnchorForRealizationRect(availableSize, context);
				anchorIndex = anchorInfo.Index;
				anchorPosition = MinorMajorPoint(0, (float)anchorInfo.Offset);
			}
			else
			{
				REPEATER_TRACE_INFO("%*s: \tConnected Window - picking first realized element as anchor.\n", context.Indent, layoutId);

				// No suggestion - just pick first in realized range
				anchorIndex = m_elementManager.GetDataIndexFromRealizedRangeIndex(0);

				if (m_elementManager.IsLayoutBoundsForRealizedIndexSet(0))
				{
					var firstRealizedElementBounds = m_elementManager.GetLayoutBoundsForRealizedIndex(0);
					anchorPosition = MinorMajorPoint((float)MinorStart(firstRealizedElementBounds), (float)MajorStart(firstRealizedElementBounds));
				}
			}
		}

		REPEATER_TRACE_INFO("%*s: \tPicked anchor:%d \n", context.Indent, layoutId, anchorIndex);
		MUX_ASSERT(anchorIndex == -1 || m_elementManager.IsIndexValidInData(anchorIndex));
		m_firstRealizedDataIndexInsideRealizationWindow = m_lastRealizedDataIndexInsideRealizationWindow = anchorIndex;
		if (m_elementManager.IsIndexValidInData(anchorIndex))
		{
			if (!m_elementManager.IsDataIndexRealized(anchorIndex))
			{
				// Disconnected, throw everything and create new anchor
				REPEATER_TRACE_INFO("%*s Disconnected Window - throwing away all realized elements \n", context.Indent, layoutId);
				m_elementManager.ClearRealizedRange();

				var anchor = m_context.GetOrCreateElementAt(anchorIndex, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
				m_elementManager.Add(anchor, anchorIndex);
			}

			var anchorElement = m_elementManager.GetRealizedElement(anchorIndex);
			var desiredSize = MeasureElement(anchorElement, anchorIndex, availableSize, m_context);
			var layoutBounds = new Rect(anchorPosition.X, anchorPosition.Y, desiredSize.Width, desiredSize.Height);
			m_elementManager.SetLayoutBoundsForDataIndex(anchorIndex, layoutBounds);

			REPEATER_TRACE_INFO("%*s: \tLayout bounds of anchor %d are (%.0f,%.0f,%.0f,%.0f). \n",
				context.Indent,
				layoutId,
				anchorIndex,
				layoutBounds.X, layoutBounds.Y, layoutBounds.Width, layoutBounds.Height);
		}
		else
		{
			// Throw everything away
			REPEATER_TRACE_INFO("%*s \tAnchor index is not valid - throwing away all realized elements \n",
				context.Indent,
				layoutId);
			m_elementManager.ClearRealizedRange();
		}

		// TODO: Perhaps we can track changes in the property setter
		m_lastAvailableSize = availableSize;
		m_lastItemSpacing = minItemSpacing;

		m_algorithmCallbacks.Algorithm_SetFlowLayoutAnchorInfoDbg(anchorIndex, Major(anchorPosition));

		return anchorIndex;
	}

	private void Generate(
		GenerateDirection direction,
		int anchorIndex,
		Size availableSize,
		bool isWrapping,
		double minItemSpacing,
		double lineSpacing,
		uint maxItemsPerLine,
		bool isVirtualizationEnabled,
		string layoutId)
	{
		if (anchorIndex != -1)
		{
			int step = (direction == GenerateDirection.Forward) ? 1 : -1;

			REPEATER_TRACE_INFO("%*s: \tGenerating %s from anchor %d. \n",
				m_context.Indent,
				layoutId,
				direction == GenerateDirection.Forward ? "forward" : "backward",
				anchorIndex);

			int previousIndex = anchorIndex;
			int currentIndex = anchorIndex + step;
			var anchorBounds = m_elementManager.GetLayoutBoundsForDataIndex(anchorIndex);
			double lineOffset = MajorStart(anchorBounds);
			double lineMajorSize = MajorSize(anchorBounds);
			uint countInLine = 1;
			bool lineNeedsReposition = false;

			if (maxItemsPerLine > 0 && maxItemsPerLine != uint.MaxValue)
			{
				// Initialize countInLine based on anchorIndex & maxItemsPerLine for a UniformGridLayout for example.
				int anchorLineIndex = (int)((uint)anchorIndex / maxItemsPerLine);
				int anchorIndexInLine = anchorIndex - (anchorLineIndex * (int)maxItemsPerLine);
				countInLine = direction == GenerateDirection.Forward ? (uint)(anchorIndexInLine + 1) : maxItemsPerLine - (uint)anchorIndexInLine;
			}

			while (m_elementManager.IsIndexValidInData(currentIndex) &&
				  (!isVirtualizationEnabled || ShouldContinueFillingUpSpace(previousIndex, direction)))
			{
				// Ensure layout element.
				m_elementManager.EnsureElementRealized(direction == GenerateDirection.Forward, currentIndex, layoutId);
				var currentElement = m_elementManager.GetRealizedElement(currentIndex);
				var desiredSize = MeasureElement(currentElement, currentIndex, availableSize, m_context);

				// Lay it out.
				var previousElement = m_elementManager.GetRealizedElement(previousIndex);
				Rect currentBounds = new Rect(0, 0, desiredSize.Width, desiredSize.Height);
				var previousElementBounds = m_elementManager.GetLayoutBoundsForDataIndex(previousIndex);

				if (direction == GenerateDirection.Forward)
				{
					double remainingSpace = Minor(availableSize) -
						(MinorStart(previousElementBounds) +
						 MinorSize(previousElementBounds) +
						 minItemSpacing +
						 Minor(desiredSize));
					if (countInLine >= maxItemsPerLine || m_algorithmCallbacks.Algorithm_ShouldBreakLine(currentIndex, remainingSpace))
					{
						// No more space in this row. wrap to next row.
						SetMinorStart(ref currentBounds, 0);
						SetMajorStart(ref currentBounds, MajorStart(previousElementBounds) + lineMajorSize + lineSpacing);

						if (lineNeedsReposition)
						{
							// reposition the previous line (countInLine items)
							for (uint i = 0; i < countInLine; i++)
							{
								var dataIndex = currentIndex - 1 - (int)i;
								var bounds = m_elementManager.GetLayoutBoundsForDataIndex(dataIndex);
								SetMajorSize(ref bounds, lineMajorSize);
								m_elementManager.SetLayoutBoundsForDataIndex(dataIndex, bounds);
							}
						}

						// Setup for next line.
						lineMajorSize = MajorSize(currentBounds);
						lineOffset = MajorStart(currentBounds);
						lineNeedsReposition = false;
						countInLine = 1;
					}
					else
					{
						// More space is available in this row.
						SetMinorStart(ref currentBounds, MinorStart(previousElementBounds) + MinorSize(previousElementBounds) + minItemSpacing);
						SetMajorStart(ref currentBounds, lineOffset);
						lineMajorSize = Math.Max(lineMajorSize, MajorSize(currentBounds));
						lineNeedsReposition = MajorSize(previousElementBounds) != MajorSize(currentBounds);
						countInLine++;
					}
				}
				else
				{
					// Backward
					double remainingSpace = MinorStart(previousElementBounds) - (Minor(desiredSize) + minItemSpacing);
					if (countInLine >= maxItemsPerLine || m_algorithmCallbacks.Algorithm_ShouldBreakLine(currentIndex, remainingSpace))
					{
						if (isWrapping)
						{
							// Does not fit, wrap to the previous row
							var availableSizeMinor = Minor(availableSize);
							// If the last available size is finite, start from end and subtract our desired size.
							// Otherwise, look at the last extent and use that for positioning.
							SetMinorStart(ref currentBounds, availableSizeMinor.IsFinite() ? availableSizeMinor - Minor(desiredSize) : MinorSize(LastExtent()) - Minor(desiredSize));
						}
						// else keep MinorStart(currentBounds) at 0. Same as for GenerateDirection::Forward.

						SetMajorStart(ref currentBounds, lineOffset - Major(desiredSize) - lineSpacing);

						if (lineNeedsReposition)
						{
							var previousLineOffset = MajorStart(m_elementManager.GetLayoutBoundsForDataIndex(currentIndex + (int)countInLine + 1));
							// reposition the previous line (countInLine items)
							for (uint i = 0; i < countInLine; i++)
							{
								var dataIndex = currentIndex + 1 + (int)i;
								if (dataIndex != anchorIndex)
								{
									var bounds = m_elementManager.GetLayoutBoundsForDataIndex(dataIndex);
									SetMajorStart(ref bounds, previousLineOffset - lineMajorSize - lineSpacing);
									SetMajorSize(ref bounds, lineMajorSize);
									m_elementManager.SetLayoutBoundsForDataIndex(dataIndex, bounds);

									REPEATER_TRACE_INFO("%*s: \t Corrected Layout bounds of element %d are (%.0f,%.0f,%.0f,%.0f). \n",
										m_context.Indent,
										layoutId,
										dataIndex,
										bounds.X, bounds.Y, bounds.Width, bounds.Height);
								}
							}
						}

						// Setup for next line.
						lineMajorSize = MajorSize(currentBounds);
						lineOffset = MajorStart(currentBounds);
						lineNeedsReposition = false;
						countInLine = 1;
					}
					else
					{
						// Fits in this row. put it in the previous position
						SetMinorStart(ref currentBounds, MinorStart(previousElementBounds) - Minor(desiredSize) - minItemSpacing);
						SetMajorStart(ref currentBounds, lineOffset);
						lineMajorSize = Math.Max(lineMajorSize, MajorSize(currentBounds));
						lineNeedsReposition = MajorSize(previousElementBounds) != MajorSize(currentBounds);
						countInLine++;
					}
				}

				m_elementManager.SetLayoutBoundsForDataIndex(currentIndex, currentBounds);

				REPEATER_TRACE_INFO("%*s: \tLayout bounds of element %d are (%.0f,%.0f,%.0f,%.0f). \n",
					m_context.Indent,
					layoutId,
					currentIndex,
					currentBounds.X, currentBounds.Y, currentBounds.Width, currentBounds.Height);

				previousIndex = currentIndex;
				currentIndex += step;
			}

			int dataCount = m_context.ItemCount;

			// If we did not reach the top or bottom of the extent, we realized one
			// extra item before we knew we were outside the realization window. Do not
			// account for that element in the indices inside the realization window.
			if (direction == GenerateDirection.Forward)
			{
				m_lastRealizedDataIndexInsideRealizationWindow = previousIndex == dataCount - 1 ? dataCount - 1 : previousIndex - 1;
				m_lastRealizedDataIndexInsideRealizationWindow = Math.Max(0, m_lastRealizedDataIndexInsideRealizationWindow);
			}
			else
			{
				m_firstRealizedDataIndexInsideRealizationWindow = previousIndex == 0 ? 0 : previousIndex + 1;
				m_firstRealizedDataIndexInsideRealizationWindow = Math.Min(dataCount - 1, m_firstRealizedDataIndexInsideRealizationWindow);
			}

			m_elementManager.DiscardElementsOutsideWindow(direction == GenerateDirection.Forward, currentIndex);
		}
	}

	private bool IsReflowRequired()
	{
		// If first element is realized and is not at the very beginning we need to reflow.
		return
			m_elementManager.GetRealizedElementCount > 0 &&
			m_elementManager.GetDataIndexFromRealizedRangeIndex(0) == 0 &&
			(GetScrollOrientation() == ScrollOrientation.Vertical
				? m_elementManager.GetLayoutBoundsForRealizedIndex(0).X != 0
				: m_elementManager.GetLayoutBoundsForRealizedIndex(0).Y != 0);
	}

	private bool ShouldContinueFillingUpSpace(
		int index,
		GenerateDirection direction)
	{
		bool shouldContinue;
		if (!IsVirtualizingContext())
		{
			shouldContinue = true;
		}
		else
		{
			var realizationRect = m_context.RealizationRect;
			var elementBounds = m_elementManager.GetLayoutBoundsForDataIndex(index);

			var elementMajorStart = MajorStart(elementBounds);
			var elementMajorEnd = MajorEnd(elementBounds);
			var rectMajorStart = MajorStart(realizationRect);
			var rectMajorEnd = MajorEnd(realizationRect);

			var elementMinorStart = MinorStart(elementBounds);
			var elementMinorEnd = MinorEnd(elementBounds);
			var rectMinorStart = MinorStart(realizationRect);
			var rectMinorEnd = MinorEnd(realizationRect);

			// Ensure that both minor and major directions are taken into consideration so that if the scrolling direction
			// is the same as the flow direction we still stop at the end of the viewport rectangle.
			shouldContinue = direction == GenerateDirection.Forward
				? elementMajorStart < rectMajorEnd && elementMinorStart < rectMinorEnd
				: elementMajorEnd > rectMajorStart && elementMinorEnd > rectMinorStart;
		}

		return shouldContinue;
	}

	private Rect EstimateExtent(
		Size availableSize,
		string layoutId)
	{
		UIElement firstRealizedElement = null;
		Rect firstRealizedBounds = default;
		UIElement lastRealizedElement = null;
		Rect lastRealizedBounds = default;
		int firstRealizedDataIndex = -1;
		int lastRealizedDataIndex = -1;

		if (m_elementManager.GetRealizedElementCount > 0)
		{
			firstRealizedElement = m_elementManager.GetAt(0);
			firstRealizedBounds = m_elementManager.GetLayoutBoundsForRealizedIndex(0);
			firstRealizedDataIndex = m_elementManager.GetDataIndexFromRealizedRangeIndex(0);

			int last = m_elementManager.GetRealizedElementCount - 1;
			lastRealizedElement = m_elementManager.GetAt(last);
			lastRealizedDataIndex = m_elementManager.GetDataIndexFromRealizedRangeIndex(last);
			lastRealizedBounds = m_elementManager.GetLayoutBoundsForRealizedIndex(last);
		}

		Rect extent = m_algorithmCallbacks.Algorithm_GetExtent(
			availableSize,
			m_context,
			firstRealizedElement,
			firstRealizedDataIndex,
			firstRealizedBounds,
			lastRealizedElement,
			lastRealizedDataIndex,
			lastRealizedBounds);

		REPEATER_TRACE_INFO("%*s Extent: (%.0f,%.0f,%.0f,%.0f). \n", m_context.Indent, layoutId, extent.X, extent.Y, extent.Width, extent.Height);

		return extent;
	}

	private Rect LayoutRound(Rect value)
	{
		MUX_ASSERT(m_layoutRoundFactor != 0.0);

		return new Rect(
			Math.Round(value.X * m_layoutRoundFactor) / m_layoutRoundFactor,
			Math.Round(value.Y * m_layoutRoundFactor) / m_layoutRoundFactor,
			Math.Round(value.Width * m_layoutRoundFactor) / m_layoutRoundFactor,
			Math.Round(value.Height * m_layoutRoundFactor) / m_layoutRoundFactor);
	}

	private void EvaluateLayoutRoundFactor(
		VirtualizingLayoutContext context,
		UIElement element)
	{
		if (element.UseLayoutRounding)
		{
			if (element.XamlRoot is { } xamlRoot)
			{
				double layoutRoundFactor = xamlRoot.RasterizationScale;

				if (layoutRoundFactor != m_layoutRoundFactor)
				{
					m_layoutRoundFactor = layoutRoundFactor;

					// This triggers a call to StackLayoutState::OnElementSizesReset()
					// for example, in the StackLayout case.
					m_algorithmCallbacks.Algorithm_OnLayoutRoundFactorChanged(context);
				}
			}
			else
			{
				m_layoutRoundFactor = 0.0;
			}
		}
		else
		{
			m_layoutRoundFactor = 0.0;
		}
	}

	private void RaiseLineArranged()
	{
		var realizationRect = RealizationRect();
		if (realizationRect.Width != 0.0f || realizationRect.Height != 0.0f)
		{
			int realizedElementCount = m_elementManager.GetRealizedElementCount;
			if (realizedElementCount > 0)
			{
				MUX_ASSERT(m_firstRealizedDataIndexInsideRealizationWindow != -1 && m_lastRealizedDataIndexInsideRealizationWindow != -1);
				int countInLine = 0;
				var previousElementBounds = m_elementManager.GetLayoutBoundsForDataIndex(m_firstRealizedDataIndexInsideRealizationWindow);
				var currentLineOffset = MajorStart(previousElementBounds);
				var currentLineSize = MajorSize(previousElementBounds);
				for (int currentDataIndex = m_firstRealizedDataIndexInsideRealizationWindow; currentDataIndex <= m_lastRealizedDataIndexInsideRealizationWindow; currentDataIndex++)
				{
					var currentBounds = m_elementManager.GetLayoutBoundsForDataIndex(currentDataIndex);
					if (MajorStart(currentBounds) != currentLineOffset)
					{
						// Starting a new line
						m_algorithmCallbacks.Algorithm_OnLineArranged(currentDataIndex - countInLine, countInLine, currentLineSize, m_context);
						countInLine = 0;
						currentLineOffset = MajorStart(currentBounds);
						currentLineSize = 0;
					}

					currentLineSize = Math.Max(currentLineSize, MajorSize(currentBounds));
					countInLine++;
					previousElementBounds = currentBounds;
				}

				// Raise for the last line.
				m_algorithmCallbacks.Algorithm_OnLineArranged(m_lastRealizedDataIndexInsideRealizationWindow - countInLine + 1, countInLine, currentLineSize, m_context);
			}
		}
	}

	// #pragma endregion

	// #pragma region Arrange related private methods

	private void ArrangeVirtualizingLayout(
		Size finalSize,
		FlowLayoutAlgorithm.LineAlignment lineAlignment,
		bool isWrapping,
		string layoutId)
	{
		// Walk through the realized elements one line at a time and
		// align them. Then call element.Arrange with the arranged bounds.
		int realizedElementCount = m_elementManager.GetRealizedElementCount;

		if (realizedElementCount > 0)
		{
			int countInLine = 1;
			var previousElementBounds = m_elementManager.GetLayoutBoundsForRealizedIndex(0);
			var currentLineOffset = MajorStart(previousElementBounds);
			var spaceAtLineStart = MinorStart(previousElementBounds);
			double spaceAtLineEnd = 0;
			double currentLineSize = MajorSize(previousElementBounds);

			for (int i = 1; i < realizedElementCount; i++)
			{
				var currentBounds = m_elementManager.GetLayoutBoundsForRealizedIndex(i);

				if (MajorStart(currentBounds) != currentLineOffset)
				{
					spaceAtLineEnd = Minor(finalSize) - MinorStart(previousElementBounds) - MinorSize(previousElementBounds);
					PerformLineAlignment(i - countInLine, countInLine, (float)spaceAtLineStart, (float)spaceAtLineEnd, (float)currentLineSize, lineAlignment, isWrapping, finalSize, layoutId);
					spaceAtLineStart = MinorStart(currentBounds);
					countInLine = 0;
					currentLineOffset = MajorStart(currentBounds);
					currentLineSize = 0;
				}

				countInLine++; // for current element
				currentLineSize = Math.Max(currentLineSize, MajorSize(currentBounds));
				previousElementBounds = currentBounds;
			}

			// Last line - potentially have a property to customize
			// aligning the last line or not.
			if (countInLine > 0)
			{
				var spaceAtEnd = Minor(finalSize) - MinorStart(previousElementBounds) - MinorSize(previousElementBounds);

				PerformLineAlignment(realizedElementCount - countInLine, countInLine, (float)spaceAtLineStart, (float)spaceAtEnd, (float)currentLineSize, lineAlignment, isWrapping, finalSize, layoutId);
			}
		}
	}

	// Align elements within a line. Note that this does not modify LayoutBounds. So if we get
	// repeated measures, the LayoutBounds remain the same in each layout.
	private void PerformLineAlignment(
		int lineStartIndex,
		int countInLine,
		float spaceAtLineStart,
		float spaceAtLineEnd,
		float lineSize,
		FlowLayoutAlgorithm.LineAlignment lineAlignment,
		bool isWrapping,
		Size finalSize,
		string layoutId)
	{
		for (int rangeIndex = lineStartIndex; rangeIndex < lineStartIndex + countInLine; ++rangeIndex)
		{
			var bounds = m_elementManager.GetLayoutBoundsForRealizedIndex(rangeIndex);
			SetMajorSize(ref bounds, lineSize);

			if (!m_scrollOrientationSameAsFlow)
			{
				// Note: Space at start could potentially be negative
				if (spaceAtLineStart != 0 || spaceAtLineEnd != 0)
				{
					float totalSpace = spaceAtLineStart + spaceAtLineEnd;
					switch (lineAlignment)
					{
						case FlowLayoutAlgorithm.LineAlignment.Start:
							{
								AddMinorStart(ref bounds, -spaceAtLineStart);
								break;
							}

						case FlowLayoutAlgorithm.LineAlignment.End:
							{
								AddMinorStart(ref bounds, spaceAtLineEnd);
								break;
							}

						case FlowLayoutAlgorithm.LineAlignment.Center:
							{
								AddMinorStart(ref bounds, -spaceAtLineStart);
								AddMinorStart(ref bounds, totalSpace / 2);
								break;
							}

						case FlowLayoutAlgorithm.LineAlignment.SpaceAround:
							{
								float interItemSpace = countInLine >= 1 ? totalSpace / (countInLine * 2) : 0;
								AddMinorStart(ref bounds, -spaceAtLineStart);
								AddMinorStart(ref bounds, interItemSpace * ((rangeIndex - lineStartIndex + 1) * 2 - 1));
								break;
							}

						case FlowLayoutAlgorithm.LineAlignment.SpaceBetween:
							{
								float interItemSpace = countInLine > 1 ? totalSpace / (countInLine - 1) : 0;
								AddMinorStart(ref bounds, -spaceAtLineStart);
								AddMinorStart(ref bounds, interItemSpace * (rangeIndex - lineStartIndex));
								break;
							}

						case FlowLayoutAlgorithm.LineAlignment.SpaceEvenly:
							{
								float interItemSpace = countInLine >= 1 ? totalSpace / (countInLine + 1) : 0;
								AddMinorStart(ref bounds, -spaceAtLineStart);
								AddMinorStart(ref bounds, interItemSpace * (rangeIndex - lineStartIndex + 1));
								break;
							}
					}
				}
			}

			bounds.X -= m_lastExtent.X;
			bounds.Y -= m_lastExtent.Y;

			if (!isWrapping)
			{
				SetMinorSize(ref bounds, Math.Max(MinorSize(bounds), Minor(finalSize)));
			}

			var element = m_elementManager.GetAt(rangeIndex);

			REPEATER_TRACE_INFO("%*s: \tArranging element %d at (%.0f,%.0f,%.0f,%.0f). \n",
				m_context.Indent,
				layoutId,
				m_elementManager.GetDataIndexFromRealizedRangeIndex(rangeIndex),
				bounds.X, bounds.Y, bounds.Width, bounds.Height);

			element.Arrange(bounds);
		}
	}

	// #pragma endregion

	// #pragma region Layout Context Helpers

	private Rect RealizationRect() => IsVirtualizingContext()
		? m_context.RealizationRect
		: new Rect(0, 0, double.PositiveInfinity, double.PositiveInfinity);

	private void SetLayoutOrigin()
	{
		if (IsVirtualizingContext())
		{
			m_context.LayoutOrigin = new Point(m_lastExtent.X, m_lastExtent.Y);
		}
		else
		{
			// Should have 0 origin for non-virtualizing layout since we always start from
			// the first item
			MUX_ASSERT(m_lastExtent.X == 0 && m_lastExtent.Y == 0);
		}
	}

	internal UIElement GetElementIfRealized(int dataIndex)
	{
		if (m_elementManager.IsDataIndexRealized(dataIndex))
		{
			return m_elementManager.GetRealizedElement(dataIndex);
		}

		return null;
	}

	private bool IsVirtualizingContext()
	{
		if (m_context != null)
		{
			var rect = m_context.RealizationRect;
			bool hasInfiniteSize = (double.IsInfinity(rect.Height) || double.IsInfinity(rect.Width));
			return !hasInfiniteSize;
		}
		return false;
	}

	// #pragma endregion
}
