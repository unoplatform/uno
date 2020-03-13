using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.DataBinding;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class ItemsStackPanel
	{
		protected override Size MeasureOverride(Size availableSize) => _layout.MeasureOverride(availableSize);

		protected override Size ArrangeOverride(Size finalSize) => _layout.ArrangeOverride(finalSize);
	}
}
