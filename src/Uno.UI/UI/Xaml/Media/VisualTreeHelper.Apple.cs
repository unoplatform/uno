#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Foundation.Logging;
using UIKit;
using _View = UIKit.UIView;
using Uno.Extensions;

namespace Microsoft.UI.Xaml.Media
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
				currentSuperview?.InsertSubview(newView, currentPosition);
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
