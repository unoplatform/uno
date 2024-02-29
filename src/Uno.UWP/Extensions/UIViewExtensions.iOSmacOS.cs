#if __IOS__
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#endif

namespace Uno.Extensions
{
	public static class UIViewExtensions
	{
		public static _View? FindFirstResponder(this _View view)
		{
#if __IOS__
			if (view.IsFirstResponder)
#elif __MACOS__
			if (view.Window.FirstResponder == view)
#endif
			{
				return view;
			}
			foreach (_View subView in view.Subviews)
			{
				var firstResponder = subView.FindFirstResponder();
				if (firstResponder != null)
				{
					return firstResponder;
				}
			}
			return null;
		}
	}
}
