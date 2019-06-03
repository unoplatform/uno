using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class CarouselPanel
	{
		protected override Size MeasureOverride(Size availableSize) => _layout.MeasureOverride(availableSize);

		protected override Size ArrangeOverride(Size finalSize) => _layout.ArrangeOverride(finalSize);
	}
}
