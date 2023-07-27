using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class CarouselPanel : VirtualizingPanel
	{
		private readonly ItemsStackPanelLayout _layout = new ItemsStackPanelLayout();

		public CarouselPanel()
		{
			_layout.Initialize(this);

			// Set CacheLength small to somewhat resemble the UWP CarouselPanel. Note that CarouselPanel doesn't expose a configurable
			// CacheLength - Uno's CarouselPanel is borrowing the ItemsStackPanel virtualization strategy.
			_layout.CacheLength = 0.5;
		}

		protected override Size MeasureOverride(Size availableSize) => _layout.MeasureOverride(availableSize);

		protected override Size ArrangeOverride(Size finalSize) => _layout.ArrangeOverride(finalSize);

		private protected override VirtualizingPanelLayout GetLayouterCore() => _layout;
	}
}
