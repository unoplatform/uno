#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.Foundation.Logging;
using Uno.UI;
using _DragEventArgs = global::Microsoft.UI.Xaml.DragEventArgs;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ListViewBase
	{

		//// Phone contstants for reorder mode.
		//// If user moves pointer less than s_reorderConfirmationThresholdInDips device-independent pixels
		//// during time s_reorderConfirmationDelayInMsec from touch-down then gesture is confirm reorderable item.
		//// Otherwise it is a pan (flick).
		// UINT ListViewBase.s_reorderConfirmationDelayInMsec = 150;
		// FLOAT ListViewBase.s_reorderConfirmationThresholdInDips = 3.0f;

		const int LISTVIEWBASE_EDGE_SCROLL_EDGE_WIDTH_PX = 100;
		const int LISTVIEWBASE_EDGE_SCROLL_START_DELAY_MSEC = 50;

		// Edge scroll speed varies linearly with distance from edge.
		// At 0 px from the edge, the speed is LISTVIEWBASE_EDGE_SCROLL_MAX_SPEED.
		// At LISTVIEWBASE_EDGE_SCROLL_EDGE_WIDTH_PX from the edge, the speed is
		// LISTVIEWBASE_EDGE_SCROLL_MIN_SPEED.
		const double LISTVIEWBASE_EDGE_SCROLL_MIN_SPEED = (150.0 /* px/sec */);
		const double LISTVIEWBASE_EDGE_SCROLL_MAX_SPEED = (1500.0 /* px/sec */);

		// The delay time before going playing the live reorder
		//const int LISTVIEW_LIVEREORDER_TIMER = 200;
		//const int GRIDVIEW_LIVEREORDER_TIMER = 300;

		// Returns true if
		// 1) a drag and drop operation is in progress, and
		// 2a) the given item is the item the user is physically dragging
		//    (as opposed to just part of the selection), or
		// 2b) the given item is the owning GroupItem for the item the user is physically dragging.
		// Sets pResult to false otherwise.
		internal bool IsContainerDragDropOwner(UIElement pContainer)
		{
			var pResult = false;

			if (m_tpPrimaryDraggedContainer != null)
			{
				// pContainer could either be a real item, or it could be a GroupItem.
				var isPrimaryDraggedContainer = Equals(pContainer, m_tpPrimaryDraggedContainer);
				if (isPrimaryDraggedContainer)
				{
					// Normal item.
					pResult = true;
				}
				// Uno TODO: support group drag
				//else
				//{
				//	// Possibly group item.
				//	IGroupItem spGroupItem = ctl.query_interface_cast<IGroupItem, UIElement>(pContainer);
				//	if (spGroupItem)
				//	{
				//		// See if the dragged container is within the group.
				//		INT groupIndex = -1;
				//		UINT groupLeftIndex = 0;
				//		UINT groupSize = 0;
				//		bool foundGroup = false;

				//		(spGroupItem as GroupItem.GetGroupIndex(&groupIndex));

				//		if (groupIndex >= 0)
				//		{
				//			(GetGroupInformation(
				//				groupIndex,
				//				&groupLeftIndex,
				//				&groupSize,
				//				&foundGroup));
				//		}

				//		// We expect the given GroupItem to still be in our collection.
				//		// If this isn't the case, let the container be recycled.
				//		ASSERT(foundGroup);
				//		if (foundGroup)
				//		{
				//			INT draggedItemIndex = 0;
				//			(IndexFromContainer(m_tpPrimaryDraggedContainer as ListViewBaseItem, &draggedItemIndex));

				//			// Check to see if the item is in the group.
				//			*pResult = ((UINT)(draggedItemIndex) >= groupLeftIndex) && ((UINT)(draggedItemIndex) < groupLeftIndex + groupSize);
				//		}
				//	}
				//}
			}

			return pResult;
		}

		// Returns true if a drag and drop is in progress.
		internal bool IsInDragDrop()
		{
			return m_tpPrimaryDraggedContainer != null;
		}

		// MUX Reference ListViewBase_Partial_Interaction.cpp, lines 3212-3215
		internal bool IsInExclusiveInteraction() => IsInDragDrop();

		// If a drag and drop operation is in progress, returns the number of items being dragged.
		// Otherwise, returns 0.
		internal int DragItemsCount()
		{
			//ASSERT((m_dragItemsCount != 0) == IsInDragDrop());
			return m_dragItemsCount;
		}

		// Returns the edge scrolling velocity we should be performing.
		// dragPoint - The point over which the user is dragging, relative to the ListViewBase.
		PanVelocity ComputeEdgeScrollVelocity(
			Point dragPoint)
		{
			bool isHorizontalScrollAllowed = false;
			bool isVerticalScrollAllowed = false;

			var velocity = new PanVelocity();

			// See in which directions we've enabled panning.
			if (m_tpScrollViewer != null)
			{
				ScrollMode verticalScrollMode = ScrollMode.Disabled;
				ScrollMode horizontalScrollMode = ScrollMode.Disabled;

				verticalScrollMode = (m_tpScrollViewer.VerticalScrollMode);
				horizontalScrollMode = (m_tpScrollViewer.HorizontalScrollMode);

				isVerticalScrollAllowed = verticalScrollMode != ScrollMode.Disabled;
				isHorizontalScrollAllowed = horizontalScrollMode != ScrollMode.Disabled;

				if (isHorizontalScrollAllowed)
				{
					double offset = 0.0;
					double bound = 0.0;

					offset = (m_tpScrollViewer.HorizontalOffset);

					// Try scrolling left.
					velocity.HorizontalVelocity = ComputeEdgeScrollVelocityFromEdgeDistance(dragPoint.X);
					if (velocity.IsStationary())
					{
						// Try scrolling right.
						double width = ActualWidth;
						velocity.HorizontalVelocity = -ComputeEdgeScrollVelocityFromEdgeDistance(width - dragPoint.X);
						bound = (m_tpScrollViewer.ScrollableWidth);
					}
					else
					{
						// We're scrolling to the left.
						// The minimum horizontal offset obtained here accounts for the presence of the
						// sentinel offset values in the left padding and/or header case. For instance,
						// with no header or padding this will return exactly 2.0.
						bound = (m_tpScrollViewer.MinHorizontalOffset);
					}

					// Disallow edge scrolling if we're right up against the edge.
					if (DoubleUtil.AreWithinTolerance(bound, offset, /*ScrollViewerScrollRoundingTolerance*/0.05))
					{
						velocity.Clear();
					}
				}

				if (isVerticalScrollAllowed && velocity.IsStationary() /* only allow vertical edge scrolling if there is no horizontal edge scrolling */)
				{
					double offset = 0.0;
					double bound = 0.0;
					offset = m_tpScrollViewer.VerticalOffset;

					// Try scrolling up.
					velocity.VerticalVelocity = ComputeEdgeScrollVelocityFromEdgeDistance(dragPoint.Y);
					if (velocity.IsStationary())
					{
						// Try scrolling down.
						double height = ActualHeight;
						velocity.VerticalVelocity = -ComputeEdgeScrollVelocityFromEdgeDistance(height - dragPoint.Y);
						bound = (m_tpScrollViewer.ScrollableHeight);
					}
					else
					{
						// We're scrolling up.
						// The minimum vertical offset obtained here accounts for the presence of the
						// sentinel offset values in the top padding and/or header case. For instance,
						// with no header or padding this will return exactly 2.0.
						bound = m_tpScrollViewer.MinVerticalOffset;
					}

					// Disallow edge scrolling if we're right up against the edge.
					if (DoubleUtil.AreWithinTolerance(bound, offset, /*ScrollViewerScrollRoundingTolerance*/0.05))
					{
						velocity.Clear();
					}
				}
			}

			return velocity;
		}

		// Computes the speed of an edge scroll, given a distance from the edge.
		float ComputeEdgeScrollVelocityFromEdgeDistance(
			 double distanceFromEdge)
		{
			if (distanceFromEdge <= LISTVIEWBASE_EDGE_SCROLL_EDGE_WIDTH_PX)
			{
				// Linear velocity gradient.
				// 0 distance:                                      LISTVIEWBASE_EDGE_SCROLL_MAX_SPEED
				// LISTVIEWBASE_EDGE_SCROLL_EDGE_WIDTH_PX distance: LISTVIEWBASE_EDGE_SCROLL_MIN_SPEED
				return (float)(LISTVIEWBASE_EDGE_SCROLL_MAX_SPEED -
								(distanceFromEdge / LISTVIEWBASE_EDGE_SCROLL_EDGE_WIDTH_PX) * (LISTVIEWBASE_EDGE_SCROLL_MAX_SPEED - LISTVIEWBASE_EDGE_SCROLL_MIN_SPEED));
			}
			else
			{
				return 0;
			}
		}

		// Implements the delay-start, but instant-update behavior for edge scrolling.
		// If the given velocity is zero, immediately stop edge scrolling.
		// If there isn't a currently running edge scroll, start a timer. When that timer
		// completes, call ScrollWithVelocity with the given arguments.
		// If there is a currently running edge scroll, change the velocity immediately.
		void SetPendingAutoPanVelocity(PanVelocity velocity)
		{

			if (!velocity.IsStationary())
			{
				if (m_currentAutoPanVelocity.IsStationary())
				{
					// Means we need to start a timer,
					// or we're updating the pending velocity.
					m_pendingAutoPanVelocity = velocity;
					EnsureStartEdgeScrollTimer();
				}
				else
				{
					// Our velocity is changing.
					m_currentAutoPanVelocity = velocity;
					ScrollWithVelocity(m_currentAutoPanVelocity);
				}
			}
			else
			{
				DestroyStartEdgeScrollTimer();
				m_currentAutoPanVelocity.Clear();
				m_pendingAutoPanVelocity.Clear();
				ScrollWithVelocity(m_currentAutoPanVelocity);
			}
		}

		// Scroll our ScrollViewer with the given velocity.
		void ScrollWithVelocity(PanVelocity velocity)
		{
			if (m_tpScrollViewer != null)
			{
				m_tpScrollViewer.SetConstantVelocities(velocity.HorizontalVelocity, velocity.VerticalVelocity);
			}
		}

		// Instantiate the edge scroll timer, if necessary, and start it.
		void EnsureStartEdgeScrollTimer()
		{
			if (m_tpStartEdgeScrollTimer == null)
			{
				var edgeScrollTimeSpan = TimeSpan.FromMilliseconds(LISTVIEWBASE_EDGE_SCROLL_START_DELAY_MSEC);

				m_tpStartEdgeScrollTimer = new DispatcherTimer();

				m_tpStartEdgeScrollTimer.Tick += StartEdgeScrollTimerTick;
				m_tpStartEdgeScrollTimer.Interval = edgeScrollTimeSpan;
				m_tpStartEdgeScrollTimer.Start();
			}
		}

		// Stop and releas the edge scroll timer.
		void DestroyStartEdgeScrollTimer()
		{
			if (m_tpStartEdgeScrollTimer != null)
			{
				m_tpStartEdgeScrollTimer.Stop();
			}

			m_tpStartEdgeScrollTimer = null;
		}

		// The edge scroll timer calls this function when it's time
		// to do our pending edge scroll action.
		void StartEdgeScrollTimerTick(
			object? pUnused1,
			object pUnused2)
		{
			DestroyStartEdgeScrollTimer();
			m_currentAutoPanVelocity = m_pendingAutoPanVelocity;
			m_pendingAutoPanVelocity.Clear();
			ScrollWithVelocity(m_currentAutoPanVelocity);
		}

		// Returns whether the passed item is the one being dragged over
		internal bool IsDragOverItem(SelectorItem pItem)
		{
			return (pItem == m_tpDragOverItem);
		}

		// MUX Reference ListViewBase_Partial_Reorder.cpp, lines 595-608
		private void ClearPrimaryDragContainer()
		{
			if (m_tpPrimaryDraggedContainer is ListViewBaseItem primaryDraggedContainer)
			{
				// Update our item's TemplateSettings with a zero DragItemsCount.
				primaryDraggedContainer.SetDragItemsCountDisplay(0);
				primaryDraggedContainer.ClearLastTouchPressedArgs();
			}

			m_tpPrimaryDraggedContainer = null;

			m_dragItemsCount = 0;
		}

		// MUX Reference ListViewBase_Partial_Reorder.cpp, lines 1153-1197 (OnDrop)
		protected override void OnDrop(_DragEventArgs args)
		{
			// TODO Uno: the reorder drop (OnReorderDrop + CompleteDrop) runs in OnReorderCompleted and OnItemContainerDragCompleted.
			base.OnDrop(args);

			if (m_tpDragOverItem is not null)
			{
				// Reset the dragged-over item and potentially switch to the pointer-over visual state.
				m_tpDragOverItem.LeaveDragOver(this, args);
			}
		}

		// MUX Reference ListViewBase_Partial_Reorder.cpp, lines 2328-2342
		internal Orientation GetPanelOrientation()
		{
			// Get the panel's logical orientation
			// We need this to know the orientation of the folder's regions
			return ItemsPanelRoot switch
			{
				// Uno-specific: ItemsStackPanel and ItemsWrapGrid don't implement IOrientedPanel; their logical orientation is Orientation.
				ItemsStackPanel itemsStackPanel => itemsStackPanel.Orientation,
				ItemsWrapGrid itemsWrapGrid => itemsWrapGrid.Orientation,
				_ => GetItemsHostOrientations().LogicalOrientation,
			};
		}

		// MUX Reference ListViewBase_Partial_Reorder.cpp, lines 2344-2357
		internal void SetDragOverItem(ListViewBaseItem? dragOverItem) => m_tpDragOverItem = dragOverItem;

		// MUX Reference ListViewBase_Partial_Interaction.cpp, lines 3172-3208
		internal void SetHoldingItem(ListViewBaseItem pItem)
		{
			if (m_tpHoldingItem != pItem)
			{
				ClearHoldingState();
				m_tpHoldingItem = pItem;
			}
		}

		internal void ClearHoldingItem(ListViewBaseItem pItem)
		{
			if (m_tpHoldingItem == pItem)
			{
				m_tpHoldingItem = null;
			}
		}

		// TODO Uno: WinUI also calls this when a DirectManipulation starts (ListViewBase_Partial.cpp:988); Uno relies on the Holding Canceled gesture.
		internal void ClearHoldingState()
		{
			if (m_tpHoldingItem is { } holdingItem)
			{
				holdingItem.ClearHoldingState();
				ClearHoldingItem(holdingItem);
				SetIsHolding(false);
			}
		}
	}
}
