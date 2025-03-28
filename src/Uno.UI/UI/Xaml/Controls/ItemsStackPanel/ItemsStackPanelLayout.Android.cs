using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using AndroidX.RecyclerView.Widget;
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

		private protected override Line CreateLine(GeneratorDirection direction,
			int extentOffset,
			int breadthOffset,
			int availableBreadth,
			RecyclerView.Recycler recycler,
			RecyclerView.State state,
			Uno.UI.IndexPath nextVisibleItem,
			bool isNewGroup
		)
		{
			if (ShouldInsertReorderingView(direction, extentOffset))
			{
				nextVisibleItem = GetAndUpdateReorderingIndex().Value;
			}

			var item = GetFlatItemIndex(nextVisibleItem);
			var view = recycler.GetViewForPosition(item, state);
			if (!(view is SelectorItem))
			{
				throw new InvalidOperationException($"Expected {nameof(SelectorItem)} but received {view?.GetType().ToString() ?? "<null>"}");
			}
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
