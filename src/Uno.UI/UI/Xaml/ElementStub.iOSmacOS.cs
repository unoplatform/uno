using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
using UIView = AppKit.NSView;
#endif

namespace Windows.UI.Xaml
{
	public partial class ElementStub : FrameworkElement
	{
		private UIView SwapViews(UIView oldView, Func<UIView> newViewProvider)
		{
			var currentPosition = oldView?.Superview?.Subviews.IndexOf(oldView) ?? -1;

			if (currentPosition != -1)
			{
				var newContent = newViewProvider();

				var currentSuperview = oldView?.Superview;
				oldView?.RemoveFromSuperview();

#if __IOS__
				currentSuperview?.InsertSubview(newContent, currentPosition);
				return newContent;
#elif __MACOS__
				currentSuperview.AddSubview(newContent, NSWindowOrderingMode.Above, currentSuperview.Subviews[Math.Max(0, currentPosition-1)]);
				return newContent;
#endif
			}

			return null;
		}
	}
}
