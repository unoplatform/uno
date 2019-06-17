using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class CarouselPanel : VirtualizingPanel
	{
		private readonly ItemsStackPanelLayout _layout = new ItemsStackPanelLayout();

		public CarouselPanel()
		{
#if __WASM__
			_layout.Initialize(this);
#endif
		}

		private protected override VirtualizingPanelLayout GetLayouterCore() => _layout;
	}
}
