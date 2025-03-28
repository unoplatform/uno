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
			var newContent = newViewProvider();

#if __IOS__
			var currentSuperview = oldView?.Superview;

			if (currentSuperview is not null)
			{
				RaiseMaterializing();

				// Use stub relative insertion, as InsertAt uses a CALayer based
				// insertion which requires to know about the layers of the siblings
				// and the manually added layers. Some of those manual layers 
				// can be added when a background or border is set on a Panel or Border.
				currentSuperview.InsertSubviewAbove(newContent, oldView);

				oldView.RemoveFromSuperview();

				return newContent;
			}
#elif __MACOS__
			var currentPosition = oldView?.Superview?.Subviews.IndexOf(oldView) ?? -1;

			if (currentPosition != -1)
			{
				var currentSuperview = oldView?.Superview;
				oldView?.RemoveFromSuperview();

				RaiseMaterializing();
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
			}
#endif

			return null;
		}
	}
}
