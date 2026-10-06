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
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	//  Abstract:
	//      ListViewBase displays a rich, interactive collection of items.
	partial class ListViewBase
	{

		// Represents a pan velocity. Used for edge scrolling.
		private struct PanVelocity
		{
			public float HorizontalVelocity;
			public float VerticalVelocity;

			public static PanVelocity Stationary => default;

			public bool IsStationary()
			{
				return (HorizontalVelocity == 0) && (VerticalVelocity == 0);
			}

			public void Clear()
			{
				HorizontalVelocity = VerticalVelocity = 0;
			}
		};

		// If there is a drag and drop in progress, this is the number of
		// items being dragged. Outside of a drag, the value is undefined.
		int m_dragItemsCount;

		// A reference to the item the user is physically dragging (as
		// opposed to items in the selection, but not being directly
		// dragged).  If there is no drag and drop in progress, this field
		// is set to NULL.
		SelectorItem? m_tpPrimaryDraggedContainer;


		// The item that should be in a DragOver state
		ListViewBaseItem? m_tpDragOverItem;

		// The current item being interacted with through pointer events. We need to know this so we
		// can forward ManipulationStarted notifications to the item, so it can update its state.
		ListViewBaseItem? m_tpHoldingItem;

		// TRUE if any item in the ListView is in press and hold state
		bool m_isHolding;


		// Edge scrolling begins after a delay. This is the velocity we will take after
		// the delay expires.
		PanVelocity m_pendingAutoPanVelocity;

		// Our current automatic scrolling speed. See ScrollWithVelocity.
		PanVelocity m_currentAutoPanVelocity;

		// Timer that handles the delay between hitting an edge and edge scrolling
		// kicking in.
		DispatcherTimer? m_tpStartEdgeScrollTimer;



		// Gets the value of m_isHolding
		internal bool GetIsHolding() => m_isHolding;

		// Sets the value of m_isHolding
		internal void SetIsHolding(bool isHolding) => m_isHolding = isHolding;
	}
}
