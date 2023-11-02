#nullable enable

using System.Collections.Generic;
using Windows.UI.Xaml;
using System;


#if __IOS__
using UIKit;
using View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
#endif


namespace Uno.UI
{
	public static partial class ViewExtensions
	{
		public static string ShowLocalVisualTree(this UIElement element, int fromHeight = 0)
		{
			return UIViewExtensions.ShowLocalVisualTree(element as View, fromHeight);
		}

		internal static IEnumerable<View> GetChildren(this UIElement element) => element.ChildrenShadow;

		internal static View? FindLastChild<T>(this View group, T param, Func<View, T, bool> predicate)
		{
			var subviews = group.Subviews;
			for (int i = subviews.Length - 1; i >= 0; i--)
			{
				if (predicate(subviews[i], param))
				{
					return subviews[i];
				}
			}

			return null;
		}
	}
}
