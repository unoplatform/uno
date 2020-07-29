using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.Extensions.Logging;
using Uno.Logging;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml.Controls
{
	partial class ScrollViewer : ICustomClippingElement
	{
		internal Size ScrollBarSize => (_presenter as ScrollContentPresenter)?.ScrollBarSize ?? default;

		private void UpdateZoomedContentAlignment()
		{
		}

		// Disable clipping for Scrollviewer (edge seems to disable scrolling if 
		// the clipping is enabled to the size of the scrollviewer, even if overflow-y is auto)
		bool ICustomClippingElement.AllowClippingToLayoutSlot => false;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;

		partial void ChangeViewScroll(double? horizontalOffset, double? verticalOffset, bool disableAnimation)
		{
			if (_presenter != null)
			{
				_presenter.ScrollTo(horizontalOffset, verticalOffset, disableAnimation);
			}
			else if (_log.IsEnabled(LogLevel.Warning))
			{
				_log.Warn("Cannot ChangeView as ScrollContentPresenter is not ready yet.");
			}
		}
	}
}
