// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollData.cpp/.h, commit 5f9e85113

#nullable disable

using Microsoft.UI.Xaml.Controls;
using Uno.UI.DataBinding;
using Windows.Foundation;

namespace DirectUI
{
	// Simple struct to track scroll offsets in the ScrollData.
	internal struct ScrollVector
	{
		public double X;
		public double Y;
	}

	// Representation of a ScrollContentPresenter's state.
	internal class ScrollData
	{
		// Whether the view can be scrolled horizontally.
		public bool m_canHorizontallyScroll;

		// Whether the view can be scrolled vertically.
		public bool m_canVerticallyScroll;

		// Minimal value of scroll offset of content.
		public ScrollVector m_MinOffset;

		// Computed offset based on _offset set by the IScrollInfo methods.  Set
		// at the end of a successful Measure pass.  This is the offset used by
		// Arrange and exposed externally.  Thus an offset set by PageDown via
		// IScrollInfo isn't reflected publicly (e.g. via the VerticalOffset
		// property) until a Measure pass.
		public ScrollVector m_ComputedOffset;

		public ScrollVector m_ArrangedOffset;

		// ViewportSize is in {pixels x items} (or vice-versa).
		public Size m_viewport;

		// Extent is the total number of children (logical dimension) or
		// physical size
		public Size m_extent;

		// Hold onto the maximum desired size to avoid re-laying out the parent
		// ScrollViewer.
		public Size m_MaxDesiredSize;

		// The ScrollViewer whose state is represented by this data.
		private ManagedWeakReference m_wrScrollOwner;

		// Scroll offset of content.  Positive corresponds to a visually upward
		// offset.  Set by methods like LineUp, PageDown, etc.
		// Private field to ensure the put_OffsetX/put_OffsetY setters are
		// consistently used so no ScrollViewer.ViewChanging notification is missed.
		private ScrollVector m_Offset;

		// Creates a new instance of the ScrollData class.
		public ScrollData()
		{
			ClearLayout();
		}

		// Creates a new instance of the ScrollData class.
		public static ScrollData Create() => new ScrollData();

		// Clears layout generated data.  It does not clear m_wrScrollOwner, because
		// unless resetting due to a m_wrScrollOwner change, we won't get reattached.
		public void ClearLayout()
		{
			Size emptySize = default;
			ScrollVector emptyVector = default;
			m_viewport = m_extent = m_MaxDesiredSize = emptySize;
			m_Offset = m_MinOffset = m_ComputedOffset = m_ArrangedOffset = emptyVector;
		}

		// Gets or sets the ScrollViewer whose state is represented by this data.
		public IScrollOwner GetScrollOwner() => m_wrScrollOwner?.Target as IScrollOwner;

		public void SetScrollOwner(IScrollOwner pScrollOwner)
		{
			if (m_wrScrollOwner is not null)
			{
				WeakReferencePool.ReturnWeakReference(this, m_wrScrollOwner);
				m_wrScrollOwner = null;
			}

			if (pScrollOwner is not null)
			{
				m_wrScrollOwner = WeakReferencePool.RentWeakReference(this, pScrollOwner);
			}
		}

		public ScrollVector GetOffset() => m_Offset;

		public double GetOffsetX() => m_Offset.X;

		public double GetOffsetY() => m_Offset.Y;

		// Records a request for a new horizontal and vertical offset,
		// and notifies the scroll owner of this request.
		public void SetOffset(in ScrollVector offset)
		{
			var spScrollOwner = GetScrollOwner();
			if (spScrollOwner is not null)
			{
				if (!DoubleUtil.AreClose(offset.X, m_Offset.X))
				{
					spScrollOwner.NotifyHorizontalOffsetChanging(offset.X, offset.Y);
				}

				if (!DoubleUtil.AreClose(offset.Y, m_Offset.Y))
				{
					spScrollOwner.NotifyVerticalOffsetChanging(offset.X, offset.Y);
				}
			}

			m_Offset = offset;
		}

		// Records a request for a new horizontal offset,
		// and notifies the scroll owner of this request.
		public void SetOffsetX(double offset)
		{
			var spScrollOwner = GetScrollOwner();
			if (spScrollOwner is not null)
			{
				spScrollOwner.NotifyHorizontalOffsetChanging(offset, m_Offset.Y);
			}

			m_Offset.X = offset;
		}

		// Records a request for a new vertical offset,
		// and notifies the scroll owner of this request.
		public void SetOffsetY(double offset)
		{
			var spScrollOwner = GetScrollOwner();
			if (spScrollOwner is not null)
			{
				spScrollOwner.NotifyVerticalOffsetChanging(m_Offset.X, offset);
			}

			m_Offset.Y = offset;
		}
	}

	internal class OffsetMemento
	{
		private double? m_pStateDelta;
		private double? m_pStateUnusedDelta;
		private double? m_pStateCurrentOffset;
		private double? m_pStateRequestedOffset;
		private Orientation m_Orientation;
		private uint m_nRealizedItemsCount;
		private uint m_nVisualItemsCount;
		private ScrollVector m_ComputedOffset;
		private ScrollVector m_ArrangedOffset;
		private Size m_MaxDesiredSize;

		public OffsetMemento(
			Orientation orientation,
			uint realizedChildrenCount,
			uint visualChildrenCount,
			ScrollData scrollData)
		{
			m_pStateDelta = null;
			m_pStateUnusedDelta = null;
			m_pStateCurrentOffset = null;
			m_pStateRequestedOffset = null;
			m_Orientation = orientation;
			m_nRealizedItemsCount = realizedChildrenCount;
			m_nVisualItemsCount = visualChildrenCount;
			m_ComputedOffset = scrollData.m_ComputedOffset;
			m_ArrangedOffset = scrollData.m_ArrangedOffset;
			m_MaxDesiredSize = scrollData.m_MaxDesiredSize;
		}

		public double GetDelta() => m_pStateDelta ?? 0.0;

		public void SetDelta(double delta)
		{
			global::System.Diagnostics.Debug.Assert(!m_pStateDelta.HasValue);
			m_pStateDelta = delta;
		}

		public double GetUnusedDelta() => m_pStateUnusedDelta ?? 0.0;

		public void SetUnusedDelta(double unusedDelta)
		{
			global::System.Diagnostics.Debug.Assert(!m_pStateUnusedDelta.HasValue);
			m_pStateUnusedDelta = unusedDelta;
		}

		public double GetCurrentOffset() => m_pStateCurrentOffset ?? 0.0;

		public void SetCurrentOffset(double currentOffset)
		{
			global::System.Diagnostics.Debug.Assert(!m_pStateCurrentOffset.HasValue);
			m_pStateCurrentOffset = currentOffset;
		}

		public double GetRequestedOffset() => m_pStateRequestedOffset ?? 0.0;

		public void SetRequestedOffset(double requestedOffset)
		{
			global::System.Diagnostics.Debug.Assert(!m_pStateRequestedOffset.HasValue);
			m_pStateRequestedOffset = requestedOffset;
		}

		public bool Equals(OffsetMemento pOffsetMemento)
		{
			if (pOffsetMemento is null)
			{
				return false;
			}

			return pOffsetMemento.m_Orientation == m_Orientation &&
				pOffsetMemento.m_nRealizedItemsCount == m_nRealizedItemsCount &&
				pOffsetMemento.m_nVisualItemsCount == m_nVisualItemsCount &&
				pOffsetMemento.m_MaxDesiredSize.Height == m_MaxDesiredSize.Height &&
				pOffsetMemento.m_MaxDesiredSize.Width == m_MaxDesiredSize.Width &&
				(pOffsetMemento.m_Orientation == Orientation.Horizontal
					? pOffsetMemento.m_ComputedOffset.X == m_ComputedOffset.X
					: pOffsetMemento.m_ComputedOffset.Y == m_ComputedOffset.Y) &&
				(pOffsetMemento.m_Orientation == Orientation.Horizontal
					? pOffsetMemento.m_ArrangedOffset.X == m_ArrangedOffset.X
					: pOffsetMemento.m_ArrangedOffset.Y == m_ArrangedOffset.Y);
		}
	}
}
