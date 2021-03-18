using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Input;

namespace Windows.UI.Xaml.Controls
{
	partial class ScrollContentPresenter
	{
		public ScrollContentPresenter()
		{
			PointerMoved += OnPointerMoved;
		}

		private void OnPointerMoved(object sender, Input.PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
				// Prevent PointerMoved being called on parent, as on UWP.
				// Note: We only want to do this if the ScrollViewer is actually scrollable (in any direction), but at present on Android
				// PointerMoved is only raised if content is scrollable - presumably because it's in this case that the inner
				// NativeScrollContentPresenter 'blocks' the touch according to Uno's pointer logic.
				e.Handled = true;
			}
		}
	}
}
