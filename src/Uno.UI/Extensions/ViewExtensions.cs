#if __IOS__
using UIKit;
using _View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using _View = AppKit.NSView;
#elif __ANDROID__
using _View = Android.Views.ViewGroup;
#else
using _View = Windows.UI.Xaml.UIElement;
#endif

using System.Collections.Generic;
using Uno.Extensions;
using Windows.UI.Xaml;

namespace Uno.UI.Extensions
{
	public static partial class ViewExtensions
	{
		/// <summary>
		/// Get all ancestor views of <paramref name="view"/>, in order from its immediate parent to the root of the visual tree.
		/// </summary>
		public static IEnumerable<_View> GetVisualAncestry(this _View view)
		{
			var ancestor = view.GetVisualTreeParent();
			while (ancestor != null)
			{
				yield return ancestor;
				ancestor = ancestor.GetVisualTreeParent();
			}
		}
	}
}
