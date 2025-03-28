#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Foundation.Logging;

#if __IOS__
using UIKit;
using _View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using _View = AppKit.NSView;
#endif
using Uno.Extensions;

namespace Windows.UI.Xaml.Media
{
	public partial class VisualTreeHelper
	{
		internal static void SwapViews(_View oldView, _View newView)
		{
			var currentPosition = oldView?.Superview?.Subviews.IndexOf(oldView) ?? -1;

			if (currentPosition != -1)
			{
				var currentSuperview = oldView?.Superview;
				oldView?.RemoveFromSuperview();

#if __IOS__
				currentSuperview?.InsertSubview(newView, currentPosition);
#elif __MACOS__
				currentSuperview?.AddSubview(newView, NSWindowOrderingMode.Above, currentSuperview.Subviews[Math.Max(0, currentPosition - 1)]);
#endif
			}
			else
			{
				if (typeof(VisualTreeHelper).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(VisualTreeHelper).Log().LogDebug($"Unable to swap view, could not find old view's position.");
				}
			}
		}
	}
}
