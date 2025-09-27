// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ElementManager.cpp, commit 864c068

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls
{
	internal static partial class CollectionExtensions
	{
		public static bool TryGetElementAt<T>(this IList<T> collection, int index, out T element)
			where T : class
		{
			if (index < collection.Count)
			{
				element = collection[index];
				return element != default; // TODO UNO
			}
			else
			{
				element = default;
				return false;
			}
		}

		public static void AddOrInsert<T>(this IList<T> collection, int index, T element)
		{
			if (index >= collection.Count)
			{
				collection.Add(element);
			}
			else
			{
				collection.Insert(index, element);
			}
		}
	}

	internal class ElementManager
	{
		private bool IsVirtualizingContext
		{
			get
			{
				if (m_context != null)
				{
					var rect = m_context.RealizationRect;
					bool hasInfiniteSize = double.IsInfinity(rect.Height) || double.IsInfinity(rect.Width);
					return !hasInfiniteSize;
				}

				return false;
			}
		}

		public int GetRealizedElementCount => IsVirtualizingContext ? (int)(m_realizedElements.Count) : m_context.ItemCount;

		private readonly List<UIElement> m_realizedElements = new List<UIElement>();
		private readonly List<Rect> m_realizedElementLayoutBounds = new List<Rect>();
		private int m_firstRealizedDataIndex = -1;
		private VirtualizingLayoutContext m_context;

		public void SetContext(VirtualizingLayoutContext virtualContext)
		{
			m_context = virtualContext;
		}

		public void OnBeginMeasure(ScrollOrientation orientation)
		{
			if (m_context != null)
			{
				if (IsVirtualizingContext)
				{
					// We proactively clear elements laid out outside of the realization
					// rect so that they are available for reuse during the current
					// measure pass.
					// This is useful during fast panning scenarios in which the realization
					// window is constantly changing and we want to reuse elements from
					// the end that's opposite to the panning direction.
					DiscardElementsOutsideWindow(m_context.RealizationRect, orientation);
				}
				else
				{
					// UNO: This is useless in managed code where we use a List which resizes itself

					//// If we are initialized with a non-virtualizing context, make sure that
					//// we have enough space to hold the bounds for all the elements.
					//int count = m_context.ItemCount;
					//if ((int)(m_realizedElementLayoutBounds.Count) != count)
					//{
					//	// Make sure there is enough space for the bounds.
					//	// Note: We could optimize when the count becomes smaller, but keeping
					//	// it always up to date is the simplest option for now.
					//	m_realizedElementLayoutBounds.resize(count, new Rect());
					//}
				}
			}
		}

		public UIElement GetAt(int realizedIndex)
		{
			UIElement element;
			if (IsVirtualizingContext)
			{
				if (!m_realizedElements.TryGetElementAt(realizedIndex, out element))
				{
					// Sentinel. Create the element now since we need it.
					int dataIndex = GetDataIndexFromRealizedRangeIndex(realizedIndex);
					REPEATER_TRACE_INFO("Creating element for sentinal with data index %d. \n", dataIndex);
					element = m_context.GetOrCreateElementAt(dataIndex, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
					m_realizedElements[realizedIndex] = element;
				}
			}
			else
			{
				// realizedIndex and dataIndex are the same (everything is realized)
				element = m_context.GetOrCreateElementAt(realizedIndex, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
			}

			return element;
		}

		public void Add(UIElement element, int dataIndex)
		{
			MUX_ASSERT(IsVirtualizingContext);

			if (m_realizedElements.Count == 0)
			{
				m_firstRealizedDataIndex = dataIndex;
			}

			m_realizedElements.Add(element);
			m_realizedElementLayoutBounds.Add(default);
		}

		void Insert(int realizedIndex, int dataIndex, UIElement element)
		{
			MUX_ASSERT(IsVirtualizingContext);

			if (realizedIndex == 0)
			{
				m_firstRealizedDataIndex = dataIndex;
			}

			m_realizedElements.AddOrInsert(realizedIndex, element);

			// Set bounds to an invalid rect since we do not know it yet.
			m_realizedElementLayoutBounds.AddOrInsert(realizedIndex, ItemsRepeater.InvalidRect /*new Rect(-1, -1, -1, -1)*/);
		}

		void ClearRealizedRange(int realizedIndex, int count)
		{
			MUX_ASSERT(IsVirtualizingContext);

			for (int i = 0; i < count; i++)
			{
				// Clear from the edges so that ItemsRepeater can optimize on maintaining
				// realized indices without walking through all the children every time.
				int index = realizedIndex == 0 ? realizedIndex + i : (realizedIndex + count - 1) - i;
				if (m_realizedElements.TryGetElementAt(index, out var elementRef))
				{
					m_context.RecycleElement(elementRef);
				}
			}

			m_realizedElements.RemoveRange(realizedIndex, count);
			m_realizedElementLayoutBounds.RemoveRange(realizedIndex, count);

			if (realizedIndex == 0)
			{
				m_firstRealizedDataIndex = m_realizedElements.Count == 0 ? -1 : m_firstRealizedDataIndex + count;
			}
		}

		public void DiscardElementsOutsideWindow(bool forward, int startIndex)
		{
			// Remove layout elements that are outside the realized range.
			if (IsDataIndexRealized(startIndex))
			{
				MUX_ASSERT(IsVirtualizingContext);
				int rangeIndex = GetRealizedRangeIndexFromDataIndex(startIndex);

				if (forward)
				{
					ClearRealizedRange(rangeIndex, GetRealizedElementCount - rangeIndex);
				}
				else
				{
					ClearRealizedRange(0, rangeIndex + 1);
				}
			}
		}

		public void ClearRealizedRange()
		{
			MUX_ASSERT(IsVirtualizingContext);
			ClearRealizedRange(0, GetRealizedElementCount);
		}

		public Rect GetLayoutBoundsForDataIndex(int dataIndex)
		{
			int realizedIndex = GetRealizedRangeIndexFromDataIndex(dataIndex);
			return m_realizedElementLayoutBounds[realizedIndex];
		}

		public void SetLayoutBoundsForDataIndex(int dataIndex, Rect bounds)
		{
			int realizedIndex = GetRealizedRangeIndexFromDataIndex(dataIndex);
			m_realizedElementLayoutBounds[realizedIndex] = bounds;
		}

		public Rect GetLayoutBoundsForRealizedIndex(int realizedIndex)
		{
			return m_realizedElementLayoutBounds[realizedIndex];
		}

		public void SetLayoutBoundsForRealizedIndex(int realizedIndex, Rect bounds)
		{
			m_realizedElementLayoutBounds[realizedIndex] = bounds;
		}

		public bool IsDataIndexRealized(int index)
		{
			if (IsVirtualizingContext)
			{
				int realizedCount = GetRealizedElementCount;
				return
					realizedCount > 0 &&
					GetDataIndexFromRealizedRangeIndex(0) <= index &&
					GetDataIndexFromRealizedRangeIndex(realizedCount - 1) >= index;
			}
			else
			{
				// Non virtualized - everything is realized
				return index >= 0 && index < m_context.ItemCount;
			}
		}

		public bool IsIndexValidInData(int currentIndex)
		{
			return currentIndex >= 0 && currentIndex < m_context.ItemCount;
		}

		public UIElement GetRealizedElement(int dataIndex)
		{
			MUX_ASSERT(IsDataIndexRealized(dataIndex));
			return IsVirtualizingContext
				? GetAt(GetRealizedRangeIndexFromDataIndex(dataIndex))
				: m_context.GetOrCreateElementAt(dataIndex, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
		}

		public void EnsureElementRealized(bool forward, int dataIndex, string layoutId)
		{
			if (IsDataIndexRealized(dataIndex) == false)
			{
				var element = m_context.GetOrCreateElementAt(dataIndex, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);

				if (forward)
				{
					Add(element, dataIndex);
				}
				else
				{
					Insert(0, dataIndex, element);
				}

				MUX_ASSERT(IsDataIndexRealized(dataIndex));
				REPEATER_TRACE_INFO("%ls: \tCreated element for index %d. \n", layoutId, dataIndex);
			}
		}

		// Does the given window intersect the range of realized elements
		public bool IsWindowConnected(Rect window, ScrollOrientation orientation, bool scrollOrientationSameAsFlow)
		{
			MUX_ASSERT(IsVirtualizingContext);
			bool intersects = false;
			if (m_realizedElementLayoutBounds.Count > 0)
			{
				var firstElementBounds = GetLayoutBoundsForRealizedIndex(0);
				var lastElementBounds = GetLayoutBoundsForRealizedIndex(GetRealizedElementCount - 1);

				var effectiveOrientation = scrollOrientationSameAsFlow ? (orientation == ScrollOrientation.Vertical ? ScrollOrientation.Horizontal : ScrollOrientation.Vertical) : orientation;


				var windowStart = effectiveOrientation == ScrollOrientation.Vertical ? window.Y : window.X;
				var windowEnd = effectiveOrientation == ScrollOrientation.Vertical ? window.Y + window.Height : window.X + window.Width;
				var firstElementStart = effectiveOrientation == ScrollOrientation.Vertical ? firstElementBounds.Y : firstElementBounds.X;
				var lastElementEnd = effectiveOrientation == ScrollOrientation.Vertical ? lastElementBounds.Y + lastElementBounds.Height : lastElementBounds.X + lastElementBounds.Width;

				intersects =
					firstElementStart <= windowEnd &&
					lastElementEnd >= windowStart;
			}

			return intersects;
		}

		public void DataSourceChanged(object source, NotifyCollectionChangedEventArgs args)
		{
			MUX_ASSERT(IsVirtualizingContext);
			if (m_realizedElements.Count > 0)
			{
				switch (args.Action)
				{
					case NotifyCollectionChangedAction.Add:
						{
							OnItemsAdded(args.NewStartingIndex, args.NewItems.Count);
						}
						break;

					case NotifyCollectionChangedAction.Replace:
						{
							int oldSize = args.OldItems.Count;
							int newSize = args.NewItems.Count;
							int oldStartIndex = args.OldStartingIndex;
							int newStartIndex = args.NewStartingIndex;

							if (oldSize == newSize &&
								oldStartIndex == newStartIndex &&
								IsDataIndexRealized(oldStartIndex) &&
								IsDataIndexRealized(oldStartIndex + oldSize - 1))
							{
								// Straight up replace of n items within the realization window.
								// Removing and adding might causes us to lose the anchor causing us
								// to throw away all containers and start from scratch.
								// Instead, we can just clear those items and set the element to
								// null (sentinel) and let the next measure get new containers for them.
								var startRealizedIndex = GetRealizedRangeIndexFromDataIndex(oldStartIndex);
								for (int realizedIndex = startRealizedIndex; realizedIndex < startRealizedIndex + oldSize; realizedIndex++)
								{
									if (m_realizedElements.TryGetElementAt(realizedIndex, out var elementRef))
									{
										m_context.RecycleElement(elementRef);
										m_realizedElements[realizedIndex] = null; // TODO UNO .... weird!!!
									}
								}
							}
							else
							{
								OnItemsRemoved(oldStartIndex, oldSize);
								OnItemsAdded(newStartIndex, newSize);
							}
						}
						break;

					case NotifyCollectionChangedAction.Remove:
						{
							OnItemsRemoved(args.OldStartingIndex, args.OldItems.Count);
						}
						break;

					case NotifyCollectionChangedAction.Reset:
						ClearRealizedRange();
						break;

					case NotifyCollectionChangedAction.Move:
						// Move is not supported by default by ItemsRepeater (throw new NotImplementedException();)
						// This is Uno specific
						OnItemsRemoved(args.OldStartingIndex, args.OldItems.Count);
						OnItemsAdded(args.NewStartingIndex, args.NewItems.Count);
						break;
				}
			}
		}

#if false
		int GetElementDataIndex(UIElement suggestedAnchor)
		{
			MUX_ASSERT(suggestedAnchor != null);
			var it = m_realizedElements.IndexOf(suggestedAnchor);
			return it != -1
				? GetDataIndexFromRealizedRangeIndex(it)
				: -1;
		}
#endif

		public int GetDataIndexFromRealizedRangeIndex(int rangeIndex)
		{
			MUX_ASSERT(rangeIndex >= 0 && rangeIndex < GetRealizedElementCount);
			return IsVirtualizingContext ? rangeIndex + m_firstRealizedDataIndex : rangeIndex;
		}

		int GetRealizedRangeIndexFromDataIndex(int dataIndex)
		{
			MUX_ASSERT(IsDataIndexRealized(dataIndex));
			return IsVirtualizingContext ? dataIndex - m_firstRealizedDataIndex : dataIndex;
		}

		void DiscardElementsOutsideWindow(Rect window, ScrollOrientation orientation)
		{
			MUX_ASSERT(IsVirtualizingContext);
			MUX_ASSERT(m_realizedElements.Count == m_realizedElementLayoutBounds.Count);

			// The following illustration explains the cutoff indices.
			// We will clear all the realized elements from both ends
			// up to the corresponding cutoff index.
			// '-' means the element is outside the cutoff range.
			// '*' means the element is inside the cutoff range and will be cleared.
			//
			// Window:
			//        |______________________________|
			// Realization range:
			// |*****----------------------------------*********|
			//      |                                  |
			//  frontCutoffIndex                backCutoffIndex
			//
			// Note that we tolerate at most one element outside of the window
			// because the FlowLayoutAlgorithm.Generate routine stops *after*
			// it laid out an element outside the realization window.
			// This is also convenient because it protects the anchor
			// during a BringIntoView operation during which the anchor may
			// not be in the realization window (in fact, the realization window
			// might be empty if the BringIntoView is issued before the first
			// layout pass).

			int realizedRangeSize = GetRealizedElementCount;
			int frontCutoffIndex = -1;
			int backCutoffIndex = realizedRangeSize;

			for (int i = 0;
				i < realizedRangeSize &&
				!Intersects(window, m_realizedElementLayoutBounds[i], orientation);
				++i)
			{
				++frontCutoffIndex;
			}

			for (int i = realizedRangeSize - 1;
				i >= 0 &&
				!Intersects(window, m_realizedElementLayoutBounds[i], orientation);
				--i)
			{
				--backCutoffIndex;
			}

			if (backCutoffIndex < realizedRangeSize - 1)
			{
				ClearRealizedRange(backCutoffIndex + 1, realizedRangeSize - backCutoffIndex - 1);
			}

			if (frontCutoffIndex > 0)
			{
				ClearRealizedRange(0, Math.Min(frontCutoffIndex, GetRealizedElementCount));
			}
		}

		/* static */
		bool Intersects(Rect lhs, Rect rhs, ScrollOrientation orientation)
		{
			var lhsStart = orientation == ScrollOrientation.Vertical ? lhs.Y : lhs.X;
			var lhsEnd = orientation == ScrollOrientation.Vertical ? lhs.Y + lhs.Height : lhs.X + lhs.Width;
			var rhsStart = orientation == ScrollOrientation.Vertical ? rhs.Y : rhs.X;
			var rhsEnd = orientation == ScrollOrientation.Vertical ? rhs.Y + rhs.Height : rhs.X + rhs.Width;

			return lhsEnd >= rhsStart && lhsStart <= rhsEnd;
		}

		void OnItemsAdded(int index, int count)
		{
			// Using the old indices here (before it was updated by the collection change)
			// if the insert data index is between the first and last realized data index, we need
			// to insert items.
			int lastRealizedDataIndex = m_firstRealizedDataIndex + GetRealizedElementCount - 1;
			int newStartingIndex = index;
			if (newStartingIndex >= m_firstRealizedDataIndex &&
				newStartingIndex <= lastRealizedDataIndex)
			{
				// Inserted within the realized range
				int insertRangeStartIndex = newStartingIndex - m_firstRealizedDataIndex;
				for (int i = 0; i < count; i++)
				{
					// Insert null (sentinel) here instead of an element, that way we dont
					// end up creating a lot of elements only to be thrown out in the next layout.
					int insertRangeIndex = insertRangeStartIndex + i;
					int dataIndex = newStartingIndex + i;
					// This is to keep the contiguousness of the mapping
					Insert(insertRangeIndex, dataIndex, null);
				}
			}
			else if (index <= m_firstRealizedDataIndex)
			{
				// Items were inserted before the realized range.
				// We need to update m_firstRealizedDataIndex;
				m_firstRealizedDataIndex += count;
			}
		}

		void OnItemsRemoved(int index, int count)
		{
			int lastRealizedDataIndex = m_firstRealizedDataIndex + (int)(m_realizedElements.Count) - 1;
			int startIndex = Math.Max(m_firstRealizedDataIndex, index);
			int endIndex = Math.Min(lastRealizedDataIndex, index + count - 1);
			bool removeAffectsFirstRealizedDataIndex = (index <= m_firstRealizedDataIndex);

			if (endIndex >= startIndex)
			{
				ClearRealizedRange(GetRealizedRangeIndexFromDataIndex(startIndex), endIndex - startIndex + 1);
			}

			if (removeAffectsFirstRealizedDataIndex &&
				m_firstRealizedDataIndex != -1)
			{
				m_firstRealizedDataIndex -= count;
			}
		}
	}

}
