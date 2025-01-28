using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Uno.Extensions;
using Windows.Foundation;
using Microsoft.UI.Xaml;

using UIKit;
using _View = UIKit.UIView;
using _Event = UIKit.UIEvent;
using _Touch = UIKit.UITouch;
using _Application = UIKit.UIApplication;
using _Window = UIKit.UIWindow;
using _ScrollView = UIKit.UIScrollView;

namespace Uno.UI.Extensions
{
	public static class UIEventExtensions
	{
		public static bool IsTouchInView(this _Event evt, _View view)
		{
			return evt?.AllTouches?.AnyObject is _Touch touch && touch.IsTouchInView(view);
		}

		internal static bool IsTouchInView(this _Touch touch, _View view)
		{
			var window = _Application.SharedApplication.KeyWindow;
			var screenLocation = touch.LocationInView(window);

			var bounds = GetBounds(window, view);

			return screenLocation.X >= bounds.X
				&& screenLocation.Y >= bounds.Y
				&& screenLocation.X < bounds.Right
				&& screenLocation.Y < bounds.Bottom;
		}

		internal static UIElement FindOriginalSource(this _Touch touch)
		{
			var view = touch.View;
			while (view != null)
			{
				if (view is UIElement elt)
				{
					return elt;
				}

				view = view.Superview;
			}

			return null;
		}

		/// <summary>
		/// Determines the bounds of the provided view, including the <see cref="UIElement.Clip"/>, using the window coordinate system.
		/// </summary>
		private static CGRect GetBounds(_Window window, _View view)
		{
			CGRect? finalRect = null;

			while (view != null)
			{
				if (view is _ScrollView)
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
