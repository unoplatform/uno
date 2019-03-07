using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
#if XAMARIN_IOS
using UIKit;
using View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
#elif __ANDROID__
using View = Android.Views.View;
#elif NET46 || __WASM__
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	internal partial class PopupPanel : Panel
	{
		public Popup Popup { get; }

		public PopupPanel(Popup popup)
		{
			Popup = popup;
			Visibility = Visibility.Collapsed;
			Background = SolidColorBrushHelper.Transparent;
		}

		protected Size _lastMeasuredSize;

		protected override Size MeasureOverride(Size availableSize)
		{
			var child = this.GetChildren().FirstOrDefault();
			if (child != null)
			{
				_lastMeasuredSize = MeasureElement(child, availableSize);
			}

			// Note that we return the availableSize and not the _lastMeasuredSize. This is because this
			// Panel always take the whole screen for the dismiss layer, but it's content will not.
			return availableSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var child = this.GetChildren().FirstOrDefault();
			if (child == null)
			{
				return finalSize;
			}

			var transform = Popup.TransformToVisual(this) as MatrixTransform;
			var finalFrame = new Rect(
				(float)transform.Matrix.OffsetX + (float)Popup.HorizontalOffset,
				(float)transform.Matrix.OffsetY + (float)Popup.VerticalOffset,
				_lastMeasuredSize.Width,
				_lastMeasuredSize.Height);

			ArrangeElement(child, finalFrame);

			return finalSize;
		}
	}
}
