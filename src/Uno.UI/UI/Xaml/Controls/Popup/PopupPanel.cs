using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.ViewManagement;
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
#elif NET461 || __WASM__
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	internal partial class PopupPanel : Panel
	{
		public Popup Popup { get; }

		public PopupPanel(Popup popup)
		{
			Popup = popup ?? throw new ArgumentNullException(nameof(popup));
			Visibility = Visibility.Collapsed;

			// Required for the dismiss handling
			// This should however be customized depending of the Popup.DismissMode
			Background = new SolidColorBrush(Colors.Transparent);
		}

		protected Size _lastMeasuredSize;

		protected override Size MeasureOverride(Size availableSize)
		{
			// Usually this check is achieved by the parent, but as this Panel
			// is injected at the root (it's a subView of the Window), we make sure
			// to enforce it here.
			var isOpen = Visibility != Visibility.Collapsed;
			if (!isOpen)
			{
				availableSize = default; // 0,0
			}

			var child = this.GetChildren().FirstOrDefault();
			if (child == null)
			{
				return availableSize;
			}

			if (!isOpen || Popup.CustomLayouter == null)
			{
				_lastMeasuredSize = MeasureElement(child, availableSize);
			}
			else
			{
				var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
				visibleBounds.Width = Math.Min(availableSize.Width, visibleBounds.Width);
				visibleBounds.Height = Math.Min(availableSize.Height, visibleBounds.Height);

				_lastMeasuredSize = Popup.CustomLayouter.Measure(availableSize, visibleBounds.Size);
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Measured PopupPanel #={GetHashCode()} ({(Popup.CustomLayouter == null?"":"**using custom layouter**")}) DC={Popup.DataContext} child={child} offset={Popup.HorizontalOffset},{Popup.VerticalOffset} availableSize={availableSize} measured={_lastMeasuredSize}");
			}

			// Note that we return the availableSize and not the _lastMeasuredSize. This is because this
			// Panel always take the whole screen for the dismiss layer, but it's content will not.
			return availableSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			// Note: Here finalSize is expected to be the be the size of the window

			var size = _lastMeasuredSize;

			// Usually this check is achieved by the parent, but as this Panel
			// is injected at the root (it's a subView of the Window), we make sure
			// to enforce it here.
			var isOpen = Visibility != Visibility.Collapsed;
			if (!isOpen)
			{
				size = finalSize = default;
			}

			var child = this.GetChildren().FirstOrDefault();
			if (child == null)
			{
				return finalSize;
			}

			if (!isOpen)
			{
				ArrangeElement(child, default);

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Arranged PopupPanel #={GetHashCode()} **closed** DC={Popup.DataContext} child={child} finalSize={finalSize}");
				}
			}
			else if (Popup.CustomLayouter == null)
			{
				// Gets the location of the popup (or its Anchor) in the VisualTree, so we will align Top/Left with it
				// Note: we do not prevent overflow of the popup on any side as UWP does not!
				//		 (And actually it also lets the view appear out of the window ...)
				var anchor = Popup.Anchor ?? Popup;
				var anchorLocation = anchor.TransformToVisual(this).TransformPoint(new Point());
				var finalFrame = new Rect(
					anchorLocation.X + (float)Popup.HorizontalOffset,
					anchorLocation.Y + (float)Popup.VerticalOffset,
					size.Width,
					size.Height);

				ArrangeElement(child, finalFrame);

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Arranged PopupPanel #={GetHashCode()} DC={Popup.DataContext} child={child} popupLocation={anchorLocation} offset={Popup.HorizontalOffset},{Popup.VerticalOffset} finalSize={finalSize} childFrame={finalFrame}");
				}
			}
			else
			{
				// Defer to the popup owner the responsibility to place the popup (e.g. ComboBox)

				var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
				visibleBounds.Width = Math.Min(finalSize.Width, visibleBounds.Width);
				visibleBounds.Height = Math.Min(finalSize.Height, visibleBounds.Height);

				Popup.CustomLayouter.Arrange(finalSize, visibleBounds, _lastMeasuredSize);

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Arranged PopupPanel #={GetHashCode()} **using custom layouter** DC={Popup.DataContext} child={child} finalSize={finalSize}");
				}
			}

			return finalSize;
		}
	}
}
