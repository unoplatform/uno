#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media
{
	public partial class VisualTreeHelper
	{
		internal static void SwapViews(UIElement oldView, UIElement newView)
		{
			var currentPosition = oldView?.Superview?.Subviews.IndexOf(oldView) ?? -1;

			if (currentPosition != -1) { 
				var currentSuperview = oldView?.Superview;
				oldView?.RemoveFromSuperview();

#if __IOS__
				currentSuperview?.InsertSubview(newView, currentPosition);
#elif __MACOS__
				currentSuperview.AddSubview(newView, NSWindowOrderingMode.Above, currentSuperview.Subviews[Math.Max(0, currentPosition-1)]);
#endif
		}
	}
	}
}
