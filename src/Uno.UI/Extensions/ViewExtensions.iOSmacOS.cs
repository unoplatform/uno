#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml;
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
	}
}
