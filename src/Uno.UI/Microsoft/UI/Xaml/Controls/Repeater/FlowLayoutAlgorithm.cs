// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FlowLayoutAlgorithm.cpp, commit 5116379

using System;
using System.Collections.Specialized;
using Windows.Foundation;
using Windows.UI.Xaml;
using Uno.Extensions;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
#if HAS_UNO_WINUI
	public
#else
	internal
#endif
	partial class FlowLayoutAlgorithm : OrientationBasedMeasures
	{
		private enum GenerateDirection
		{
			Forward,
			Backward
		}

		ElementManager m_elementManager = new ElementManager();
		Size m_lastAvailableSize;
		double m_lastItemSpacing;
		bool m_collectionChangePending;
		VirtualizingLayoutContext m_context;
		IFlowLayoutAlgorithmDelegates m_algorithmCallbacks;
		Rect m_lastExtent;
		int m_firstRealizedDataIndexInsideRealizationWindow = -1;
		int m_lastRealizedDataIndexInsideRealizationWindow = -1;


		// If the scroll orientation is the same as the follow orientation
		// we will only have one line since we will never wrap. In that case
		// we do not want to align the line. We could potentially switch the
		// meaning of line alignment in this case, but I'll hold off on that
		// feature until someone asks for it - This is not a common scenario
		// anyway.
		bool m_scrollOrientationSameAsFlow;

		public Rect LastExtent => m_lastExtent;

		public void InitializeForContext(VirtualizingLayoutContext context, IFlowLayoutAlgorithmDelegates callbacks)
		{
			m_algorithmCallbacks = callbacks;
			m_context = context;
			m_elementManager.SetContext(context);
		}

		public void UninitializeForContext(VirtualizingLayoutContext context)
		{
			if (IsVirtualizingContext)
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
			bool disableVirtualization,
			string layoutId)
		{
			ScrollOrientation = orientation;

			// If minor size is infinity, there is only one line and no need to align that line.
			m_scrollOrientationSameAsFlow = Minor(availableSize) == double.PositiveInfinity;
			var realizationRect = RealizationRect;
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

			if (!disableVirtualization)
			{
				m_elementManager.OnBeginMeasure(orientation);
			}

			int anchorIndex = GetAnchorIndex(availableSize, isWrapping, minItemSpacing, disableVirtualization, layoutId);
			Generate(GenerateDirection.Forward, anchorIndex, availableSize, minItemSpacing, lineSpacing, maxItemsPerLine, disableVirtualization, layoutId);
			Generate(GenerateDirection.Backward, anchorIndex, availableSize, minItemSpacing, lineSpacing, maxItemsPerLine, disableVirtualization, layoutId);
			if (isWrapping && IsReflowRequired)
			{
				REPEATER_TRACE_INFO("%*s: \tReflow Pass \n", context.Indent, layoutId);
				var firstElementBounds = m_elementManager.GetLayoutBoundsForRealizedIndex(0);
				SetMinorStart(ref firstElementBounds, 0);
				m_elementManager.SetLayoutBoundsForRealizedIndex(0, firstElementBounds);
				Generate(GenerateDirection.Forward, 0 /*anchorIndex*/, availableSize, minItemSpacing, lineSpacing, maxItemsPerLine, disableVirtualization, layoutId);
			}

			RaiseLineArranged();
			m_collectionChangePending = false;
			m_lastExtent = EstimateExtent(availableSize, layoutId);
			SetLayoutOrigin();

			return m_lastExtent.Size;
		}

		public Size Arrange(
			Size finalSize,
			VirtualizingLayoutContext context,
			bool isWrapping,
			FlowLayoutLineAlignment lineAlignment,
			string layoutId)
		{
			REPEATER_TRACE_INFO("%*s: \tArrangeLayout \n", context.Indent, layoutId);
			ArrangeVirtualizingLayout(finalSize, lineAlignment, isWrapping, layoutId);

			return new Size(
				Math.Max(finalSize.Width, m_lastExtent.Width),
				Math.Max(finalSize.Height, m_lastExtent.Height));
		}

		void MakeAnchor(
			VirtualizingLayoutContext context,
			int index,
			Size availableSize)
		{
			m_elementManager.ClearRealizedRange();
			// FlowLayout requires that the anchor is the first element in the row.
			var internalAnchor = m_algorithmCallbacks.Algorithm_GetAnchorForTargetElement(index, availableSize, context);
			global::System.Diagnostics.Debug.Assert(internalAnchor.Index <= index);

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

		public void OnItemsSourceChanged(object source, NotifyCollectionChangedEventArgs args, VirtualizingLayoutContext context)
		{
			m_elementManager.DataSourceChanged(source, args);
			m_collectionChangePending = true;
		}

		public Size MeasureElement(UIElement element, int index, Size availableSize, VirtualizingLayoutContext context)
		{
			var measureSize = m_algorithmCallbacks.Algorithm_GetMeasureSize(index, availableSize, context);
			element.Measure(measureSize);
			var provisionalArrangeSize = m_algorithmCallbacks.Algorithm_GetProvisionalArrangeSize(index, measureSize, element.DesiredSize, context);
			m_algorithmCallbacks.Algorithm_OnElementMeasured(element, index, availableSize, measureSize, element.DesiredSize, provisionalArrangeSize, context);

			return provisionalArrangeSize;
		}

		#region Measure related private methods

		int GetAnchorIndex(
			Size availableSize,
			bool isWrapping,
			double minItemSpacing,
			bool disableVirtualization,
			string layoutId)
		{
			int anchorIndex = -1;
			Point anchorPosition = default;
			var context = m_context;

			if (!IsVirtualizingContext || disableVirtualization)
			{
				// Non virtualizing host, start generating from the element 0
				anchorIndex = context.ItemCount > 0 ? 0 : -1;
			}
			else
			{
				bool isRealizationWindowConnected = m_elementManager.IsWindowConnected(RealizationRect, ScrollOrientation, m_scrollOrientationSameAsFlow);
				// Item spacing and size in non-virtualizing direction change can cause elements to reflow
				// and get a new column position. In that case we need the anchor to be positioned in the
				// correct column.
				bool needAnchorColumnRevaluation = isWrapping && (
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
						if (needAnchorColumnRevaluation)
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
						global::System.Diagnostics.Debug.Assert(anchorIndex < firstRealizedDataIndex);
						for (int i = firstRealizedDataIndex - 1; i >= anchorIndex; --i)
						{
							m_elementManager.EnsureElementRealized(false /*forward*/, i, layoutId);
						}

						var anchorBounds = m_elementManager.GetLayoutBoundsForDataIndex(suggestedAnchorIndex);
						anchorPosition = MinorMajorPoint(0, (float)MajorStart(anchorBounds));
					}
				}
				else if (needAnchorColumnRevaluation || !isRealizationWindowConnected)
				{
					if (needAnchorColumnRevaluation) { REPEATER_TRACE_INFO("%*s: \tNeedAnchorColumnReevaluation \n", context.Indent, layoutId); }

					if (!isRealizationWindowConnected) { REPEATER_TRACE_INFO("%*s: \tDisconnected Window \n", context.Indent, layoutId); }

					// The anchor is based on the realization window because a connected ItemsRepeater might intersect the realization window
					// but not the visible window. In that situation, we still need to produce a valid anchor.
					var anchorInfo = m_algorithmCallbacks.Algorithm_GetAnchorForRealizationRect(availableSize, context);
					anchorIndex = anchorInfo.Index;
					anchorPosition = MinorMajorPoint(0, (float)(anchorInfo.Offset));
				}
				else
				{
					REPEATER_TRACE_INFO("%*s: \tConnected Window - picking first realized element as anchor \n", context.Indent, layoutId);
					// No suggestion - just pick first in realized range
					anchorIndex = m_elementManager.GetDataIndexFromRealizedRangeIndex(0);
					var firstElementBounds = m_elementManager.GetLayoutBoundsForRealizedIndex(0);
					anchorPosition = new Point(firstElementBounds.X, firstElementBounds.Y);
				}
			}

			REPEATER_TRACE_INFO("%*s: \tPicked anchor:%d \n", context.Indent, layoutId, anchorIndex);
			global::System.Diagnostics.Debug.Assert(anchorIndex == -1 || m_elementManager.IsIndexValidInData(anchorIndex));
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

			return anchorIndex;
		}


		void Generate(
			GenerateDirection direction,
			int anchorIndex,
			Size availableSize,
			double minItemSpacing,
			double lineSpacing,
			uint maxItemsPerLine,
			bool disableVirtualization,
			string layoutId)
		{
			if (anchorIndex != -1)
			{
				int step = (direction == GenerateDirection.Forward) ? 1 : -1;

				REPEATER_TRACE_INFO("%*s: \tGenerating %ls from anchor %d. \n",
					m_context.Indent,
					layoutId,
					direction == GenerateDirection.Forward ? "forward" : "backward",
					anchorIndex);

				int previousIndex = anchorIndex;
				int currentIndex = anchorIndex + step;
				var anchorBounds = m_elementManager.GetLayoutBoundsForDataIndex(anchorIndex);
				double lineOffset = MajorStart(anchorBounds);
				double lineMajorSize = MajorSize(anchorBounds);
				int countInLine = 1;
				bool lineNeedsReposition = false;

				while (m_elementManager.IsIndexValidInData(currentIndex) &&
					(disableVirtualization || ShouldContinueFillingUpSpace(previousIndex, direction)))
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
						double remainingSpace = Minor(availableSize) - (MinorStart(previousElementBounds) + MinorSize(previousElementBounds) + minItemSpacing + Minor(desiredSize));
						if (countInLine >= maxItemsPerLine || m_algorithmCallbacks.Algorithm_ShouldBreakLine(currentIndex, remainingSpace))
						{
							// No more space in this row. wrap to next row.
							SetMinorStart(ref currentBounds, 0);
							SetMajorStart(ref currentBounds, MajorStart(previousElementBounds) + lineMajorSize + (float)(lineSpacing));

							if (lineNeedsReposition)
							{
								// reposition the previous line (countInLine items)
								for (int i = 0; i < countInLine; i++)
								{
									var dataIndex = currentIndex - 1 - i;
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
							SetMinorStart(ref currentBounds, MinorStart(previousElementBounds) + MinorSize(previousElementBounds) + (float)(minItemSpacing));
							SetMajorStart(ref currentBounds, lineOffset);
							lineMajorSize = Math.Max(lineMajorSize, MajorSize(currentBounds));
							lineNeedsReposition = MajorSize(previousElementBounds) != MajorSize(currentBounds);
							countInLine++;
						}
					}
					else
					{
						// Backward
						double remainingSpace = MinorStart(previousElementBounds) - (Minor(desiredSize) + (float)(minItemSpacing));
						if (countInLine >= maxItemsPerLine || m_algorithmCallbacks.Algorithm_ShouldBreakLine(currentIndex, remainingSpace))
						{
							// Does not fit, wrap to the previous row
							var availableSizeMinor = Minor(availableSize);
							// If the last available size is finite, start from end and subtract our desired size.
							// Otherwise, look at the last extent and use that for positioning.
							SetMinorStart(ref currentBounds, availableSizeMinor.IsFinite() ? availableSizeMinor - Minor(desiredSize) : MinorSize(LastExtent) - Minor(desiredSize));
							SetMajorStart(ref currentBounds, lineOffset - Major(desiredSize) - (float)(lineSpacing));

							if (lineNeedsReposition)
							{
								var previousLineOffset = MajorStart(m_elementManager.GetLayoutBoundsForDataIndex(currentIndex + countInLine + 1));
								// reposition the previous line (countInLine items)
								for (uint i = 0; i < countInLine; i++)
								{
									var dataIndex = currentIndex + 1 + (int)i;
									if (dataIndex != anchorIndex)
									{
										var bounds = m_elementManager.GetLayoutBoundsForDataIndex(dataIndex);
										SetMajorStart(ref bounds, previousLineOffset - lineMajorSize - (float)(lineSpacing));
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
							SetMinorStart(ref currentBounds, MinorStart(previousElementBounds) - Minor(desiredSize) - (float)(minItemSpacing));
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

				// If we did not reach the top or bottom of the extent, we realized one
				// extra item before we knew we were outside the realization window. Do not
				// account for that element in the indicies inside the realization window.
				if (direction == GenerateDirection.Forward)
				{
					int dataCount = m_context.ItemCount;
					m_lastRealizedDataIndexInsideRealizationWindow = previousIndex == dataCount - 1 ? dataCount - 1 : previousIndex - 1;
					m_lastRealizedDataIndexInsideRealizationWindow = Math.Max(0, m_lastRealizedDataIndexInsideRealizationWindow);
				}
				else
				{
					int dataCount = m_context.ItemCount;
					m_firstRealizedDataIndexInsideRealizationWindow = previousIndex == 0 ? 0 : previousIndex + 1;
					m_firstRealizedDataIndexInsideRealizationWindow = Math.Min(dataCount - 1, m_firstRealizedDataIndexInsideRealizationWindow);
				}

				m_elementManager.DiscardElementsOutsideWindow(direction == GenerateDirection.Forward, currentIndex);
			}
		}

		private bool IsReflowRequired
		{
			get
			{
				// If first element is realized and is not at the very beginning we need to reflow.
				return
					m_elementManager.GetRealizedElementCount > 0 &&
									   m_elementManager.GetDataIndexFromRealizedRangeIndex(0) == 0 &&
									   (ScrollOrientation == ScrollOrientation.Vertical ? m_elementManager.GetLayoutBoundsForRealizedIndex(0).X != 0 : m_elementManager.GetLayoutBoundsForRealizedIndex(0).Y != 0);
			}
		}

		bool ShouldContinueFillingUpSpace(int index, GenerateDirection direction)
		{
			bool shouldContinue = false;
			if (!IsVirtualizingContext)
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

		Rect EstimateExtent(Size availableSize, string layoutId)
		{
			UIElement firstRealizedElement = null;
			Rect firstBounds = default;
			UIElement lastRealizedElement = null;
			Rect lastBounds = default;
			int firstDataIndex = -1;
			int lastDataIndex = -1;

			if (m_elementManager.GetRealizedElementCount > 0)
			{
				firstRealizedElement = m_elementManager.GetAt(0);
				firstBounds = m_elementManager.GetLayoutBoundsForRealizedIndex(0);
				firstDataIndex = m_elementManager.GetDataIndexFromRealizedRangeIndex(0);

				int last = m_elementManager.GetRealizedElementCount - 1;
				lastRealizedElement = m_elementManager.GetAt(last);
				lastDataIndex = m_elementManager.GetDataIndexFromRealizedRangeIndex(last);
				lastBounds = m_elementManager.GetLayoutBoundsForRealizedIndex(last);
			}

			Rect extent = m_algorithmCallbacks.Algorithm_GetExtent(
				availableSize,
				m_context,
				firstRealizedElement,
				firstDataIndex,
				firstBounds,
				lastRealizedElement,
				lastDataIndex,
				lastBounds);

			REPEATER_TRACE_INFO("%*s Extent: (%.0f,%.0f,%.0f,%.0f). \n", m_context.Indent, layoutId, extent.X, extent.Y, extent.Width, extent.Height);
			return extent;
		}

		void RaiseLineArranged()
		{
			var realizationRect = RealizationRect;
			if (realizationRect.Width != 0.0f || realizationRect.Height != 0.0f)
			{
				int realizedElementCount = m_elementManager.GetRealizedElementCount;
				if (realizedElementCount > 0)
				{
					global::System.Diagnostics.Debug.Assert(m_firstRealizedDataIndexInsideRealizationWindow != -1 && m_lastRealizedDataIndexInsideRealizationWindow != -1);
					int countInLine = 0;
					var previousElementBounds = m_elementManager.GetLayoutBoundsForDataIndex(m_firstRealizedDataIndexInsideRealizationWindow);
					var currentLineOffset = MajorStart(previousElementBounds);
					var currentLineSize = MajorSize(previousElementBounds);
					for (int currentDataIndex = m_firstRealizedDataIndexInsideRealizationWindow; currentDataIndex <= m_lastRealizedDataIndexInsideRealizationWindow; currentDataIndex++)
					{
						var currentBounds = m_elementManager.GetLayoutBoundsForDataIndex(currentDataIndex);
						if (MajorStart(currentBounds) != currentLineOffset)
						{
							// Staring a new line
							m_algorithmCallbacks.Algorithm_OnLineArranged(currentDataIndex - countInLine, countInLine, currentLineSize, m_context);
							countInLine = 0;
							currentLineOffset = MajorStart(currentBounds);
							currentLineSize = 0;
						}

						currentLineSize = Math.Max((float)(currentLineSize), MajorSize(currentBounds));
						countInLine++;
						previousElementBounds = currentBounds;
					}

					// Raise for the last line.
					m_algorithmCallbacks.Algorithm_OnLineArranged(m_lastRealizedDataIndexInsideRealizationWindow - countInLine + 1, countInLine, currentLineSize, m_context);
				}
			}
		}

		#endregion

		#region Arrange related private methods

		void ArrangeVirtualizingLayout(

		Size finalSize,
		FlowLayoutLineAlignment lineAlignment,
		bool isWrapping,
		string layoutId)
		{
			// Walk through the realized elements one line at a time and
			// align them, Then call element.Arrange with the arranged bounds.
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
		void PerformLineAlignment(
			int lineStartIndex,
			int countInLine,
			float spaceAtLineStart,
			float spaceAtLineEnd,
			float lineSize,
			FlowLayoutLineAlignment lineAlignment,
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
							case FlowLayoutLineAlignment.Start:
								{
									AddMinorStart(ref bounds, -spaceAtLineStart);
									break;
								}

							case FlowLayoutLineAlignment.End:
								{
									AddMinorStart(ref bounds, +spaceAtLineEnd);
									break;
								}

							case FlowLayoutLineAlignment.Center:
								{
									AddMinorStart(ref bounds, -spaceAtLineStart);
									AddMinorStart(ref bounds, +totalSpace / 2);
									break;
								}

							case FlowLayoutLineAlignment.SpaceAround:
								{
									float interItemSpace = countInLine >= 1 ? totalSpace / (countInLine * 2) : 0;
									AddMinorStart(ref bounds, -spaceAtLineStart);
									AddMinorStart(ref bounds, +interItemSpace * ((rangeIndex - lineStartIndex + 1) * 2 - 1));
									break;
								}

							case FlowLayoutLineAlignment.SpaceBetween:
								{
									float interItemSpace = countInLine > 1 ? totalSpace / (countInLine - 1) : 0;
									AddMinorStart(ref bounds, -spaceAtLineStart);
									AddMinorStart(ref bounds, +interItemSpace * (rangeIndex - lineStartIndex));
									break;
								}

							case FlowLayoutLineAlignment.SpaceEvenly:
								{
									float interItemSpace = countInLine >= 1 ? totalSpace / (countInLine + 1) : 0;
									AddMinorStart(ref bounds, -spaceAtLineStart);
									AddMinorStart(ref bounds, +interItemSpace * (rangeIndex - lineStartIndex + 1));
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

		#endregion

		#region Layout Context Helpers

		private Rect RealizationRect => IsVirtualizingContext
			? m_context.RealizationRect
			: new Rect(0, 0, double.PositiveInfinity, double.PositiveInfinity);

		void SetLayoutOrigin()
		{
			if (IsVirtualizingContext)
			{
				m_context.LayoutOrigin = new Point(m_lastExtent.X, m_lastExtent.Y);
			}
			else
			{
				// Should have 0 origin for non-virtualizing layout since we always start from
				// the first item
				global::System.Diagnostics.Debug.Assert(m_lastExtent.X == 0 && m_lastExtent.Y == 0);
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

#if false
		bool TryAddElement0(UIElement element)
		{
			if (m_elementManager.GetRealizedElementCount == 0)
			{
				m_elementManager.Add(element, 0);
				return true;
			}

			return false;
		}
#endif

		bool IsVirtualizingContext
		{
			get
			{
				if (m_context != null)
				{
					var rect = m_context.RealizationRect;
					bool hasInfiniteSize = (double.IsInfinity(rect.Height) || double.IsInfinity(rect.Width));
					return !hasInfiniteSize;
				}

				return false;
			}
		}

		#endregion
	}
}
