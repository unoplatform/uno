#nullable enable

using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Uno.UI
{
	public static partial class ViewExtensions
	{
		public static string ShowLocalVisualTree(this UIElement element, int fromHeight = 0)
		{
#if __MACOS__
			return AppKit.UIViewExtensions.ShowLocalVisualTree(element as AppKit.NSView, fromHeight);
#else
			return UIKit.UIViewExtensions.ShowLocalVisualTree(element as UIKit.UIView, fromHeight);
#endif
		}

#if __IOS__
		internal static IEnumerable<UIKit.UIView> GetChildren(this UIElement element) => element.ChildrenShadow;
#elif __MACOS__
		internal static IEnumerable<AppKit.NSView> GetChildren(this UIElement element) => element.ChildrenShadow;
#endif
	}
}
