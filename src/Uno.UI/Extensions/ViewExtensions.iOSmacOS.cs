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

		internal static TResult? FindLastChild<TParam, TResult>(this View group, TParam param, Func<View, TParam, TResult?> selector)
			where TResult : class
		{
			var subviews = group.Subviews;
			for (int i = subviews.Length - 1; i >= 0; i--)
			{
				var result = selector(subviews[i], param);
				if (result is not null)
				{
					return result;
				}
			}

			return null;
		}
	}
}
