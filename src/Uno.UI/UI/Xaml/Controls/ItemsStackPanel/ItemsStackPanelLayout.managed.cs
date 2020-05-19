#if !NET461
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
#if __ANDROID__ || __IOS__
	internal partial class ManagedItemsStackPanelLayout : ManagedVirtualizingPanelLayout
#else
	partial class ItemsStackPanelLayout
#endif
	{
#if __ANDROID__ || __IOS__
		public override Orientation ScrollOrientation => Orientation;
#endif

		protected override Line CreateLine(GeneratorDirection fillDirection, double extentOffset, double availableBreadth, Uno.UI.IndexPath nextVisibleItem)
		{
			var item = GetFlatItemIndex(nextVisibleItem);
			var view = Generator.DequeueViewForItem(item);

			AddView(view, fillDirection, extentOffset, 0);

			return new Line(new[] { view }, nextVisibleItem, nextVisibleItem, item);
		}

		protected override int GetItemsPerLine() => 1;
	}
}

#endif
