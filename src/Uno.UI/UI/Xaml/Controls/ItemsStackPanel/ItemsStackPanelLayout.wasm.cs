using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	partial class ItemsStackPanelLayout
	{
		protected override Line CreateLine(GeneratorDirection fillDirection, double extentOffset, double availableBreadth, IndexPath nextVisibleItem)
		{
			var item = GetFlatItemIndex(nextVisibleItem);
			var view = Generator.DequeueViewForItem(item);

			AddView(view, fillDirection, extentOffset, 0);

			return new Line(new[] { view }, nextVisibleItem, nextVisibleItem, item);
		}

		protected override int GetItemsPerLine() => 1;
	}
}
