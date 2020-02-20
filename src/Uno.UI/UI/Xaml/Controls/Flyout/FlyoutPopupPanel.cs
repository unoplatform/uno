#if !__UWP__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal partial class FlyoutBasePopupPanel : PlacementPopupPanel
	{
		private readonly FlyoutBase _flyout;

		public FlyoutBasePopupPanel(FlyoutBase flyout) : base(flyout._popup)
		{
			_flyout = flyout;

			// Required for the dismiss handling
			// This should however be customized depending of the Popup.DismissMode
			Background = new SolidColorBrush(Colors.Transparent);
		}

		protected override FlyoutPlacementMode PopupPlacement => _flyout.Placement;

		protected override FrameworkElement AnchorControl => _flyout.Target as FrameworkElement;

		internal FlyoutBase Flyout => _flyout;
	}
}
#endif
