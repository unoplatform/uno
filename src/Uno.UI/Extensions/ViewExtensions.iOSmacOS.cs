#nullable enable

using System.Collections.Generic;
using Windows.UI.Xaml;
using System;
using Uno.UI.Controls;



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

		internal static TResult? FindLastChild<TParam, TResult>(this View group, TParam param, Func<View, TParam, TResult?> selector, out bool hasAnyChildren)
			where TResult : class
		{
			hasAnyChildren = false;
			if (group is IShadowChildrenProvider shadowProvider)
			{
				var childrenShadow = shadowProvider.ChildrenShadow;
				for (int i = childrenShadow.Count - 1; i >= 0; i--)
				{
					hasAnyChildren = true;
					var result = selector(childrenShadow[i], param);
					if (result is not null)
					{
						return result;
					}
				}

				return null;
			}

			var subviews = group.Subviews;
			for (int i = subviews.Length - 1; i >= 0; i--)
			{
				hasAnyChildren = true;
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
