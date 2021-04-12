#nullable enable

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
	}
}
