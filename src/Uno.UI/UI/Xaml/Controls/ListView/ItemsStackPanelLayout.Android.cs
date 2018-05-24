using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Android.Support.V7.Widget;
using Uno.UI;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// A native layout which implements <see cref="ItemsStackPanel"/> behaviour.
	/// </summary>
	internal partial class ItemsStackPanelLayout : VirtualizingPanelLayout
	{

		protected override Line CreateLine(FillDirection direction,
			int extentOffset,
			int breadthOffset,
			int availableBreadth,
			RecyclerView.Recycler recycler,
			RecyclerView.State state,
			IndexPath nextVisibleItem,
			bool isNewGroup
		)
		{
			var item = GetFlatItemIndex(nextVisibleItem);
			var view = recycler.GetViewForPosition(item, state);
			Debug.Assert(view is SelectorItem, "view is SelectorItem (we should never be given a group header)");
			var size = AddViewAtOffset(view, direction, extentOffset, breadthOffset, availableBreadth);
			var physicalSize = size.LogicalToPhysicalPixels();

			var breadth = (int)(ScrollOrientation == Orientation.Vertical ? physicalSize.Width : physicalSize.Height);
			return new Line
			{
				NumberOfViews = 1,
				Extent = (int)(ScrollOrientation == Orientation.Vertical ? physicalSize.Height : physicalSize.Width),
				FirstItem = nextVisibleItem,
				LastItem = nextVisibleItem,
				Breadth = breadth
			};
		}
	}
}
