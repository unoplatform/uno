using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using UIKit;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Uno.UI.Extensions
{
	public static class UIEventExtensions
	{
		public static bool IsTouchInView(this UIEvent evt, UIView view)
		{
			var touch = evt?.AllTouches?.AnyObject as UITouch;

			if (touch == null)
			{
				return false;
			}
			else
			{
				var window = UIApplication.SharedApplication.KeyWindow;
				var screenLocation = touch.LocationInView(window);

				var bounds = GetBounds(window, view);

				return screenLocation.X >= bounds.X
					&& screenLocation.Y >= bounds.Y
					&& screenLocation.X < bounds.Right
					&& screenLocation.Y < bounds.Bottom;
			}
		}

		/// <summary>
		/// Determines the bounds of the provided view, including the <see cref="UIElement.Clip"/>, using the window coordinate system.
		/// </summary>
		private static CGRect GetBounds(UIWindow window, UIView view)
		{
			CGRect? finalRect = null;

			while (view != null)
			{
				if (view is UIScrollView)
				{
					// We don't support ScrollViewer clipping, because detecting 
					// the scrolling position and adjusting it makes it for unreliable 
					// calculations, for now.
					view = view.Superview;
				}
				else
				{
					var viewOnScreen = view.ConvertRectToView(view.Bounds, window);

					var element = view as FrameworkElement;

					if (element?.Clip != null)
					{
						var clip = element.Clip.Rect.ToCGRect();

						// Offset the local clip bounds to get the clip bounds on screen
						clip.Offset(viewOnScreen.X, viewOnScreen.Y);

						viewOnScreen.Intersect(clip);
					}

					if (finalRect == null)
					{
						finalRect = viewOnScreen;
					}

					var r2 = finalRect.Value;
					r2.Intersect(viewOnScreen);
					finalRect = r2;

					view = view.Superview;
				}
			}

			return finalRect.Value;
		}
	}
}
