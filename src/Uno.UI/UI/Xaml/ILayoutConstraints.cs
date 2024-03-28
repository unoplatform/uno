using System;
using System.Collections.Generic;
using System.Text;
#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#else
using _View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml
{
	/// <summary>
	/// An element that can participate in optimizations for propagating layout requests. 
	/// </summary>
	public interface ILayoutConstraints
	{
		/// <summary>
		/// Is the width of this element constrained by its Width and/or HorizontalAlignment properties and those of its ancestors.
		/// </summary>
		/// <param name="requester">The child view requesting constraint information.</param>
		/// <returns>True if the width is constrained, false if it may change when descendant elements change.</returns>
		bool IsWidthConstrained(_View requester);

		/// <summary>
		/// Is the height of this element constrained by its Height and/or VerticalAlignment properties and those of its ancestors.
		/// </summary>
		/// <param name="requester">The child view requesting constraint information.</param>
		/// <returns>True if the width is constrained, false if it may change when descendant elements change.</returns>
		bool IsHeightConstrained(_View requester);
	}
}
