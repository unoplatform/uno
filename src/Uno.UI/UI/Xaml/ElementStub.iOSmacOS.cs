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

namespace Microsoft.UI.Xaml
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

				RaiseMaterializing();

#if __IOS__
				currentSuperview?.InsertSubview(newContent, currentPosition);
				return newContent;
#elif __MACOS__
				if (currentSuperview is { })
				{
					if (currentSuperview.Subviews.Length > 0)
					{
						var position = Math.Max(0, currentPosition - 1);
						currentSuperview.AddSubview(newContent,
													NSWindowOrderingMode.Above,
													currentSuperview.Subviews[position]);
					}
					else
					{
						currentSuperview.AddSubview(newContent);
					}
				}
				return newContent;
#endif
			}

			return null;
		}
	}
}
