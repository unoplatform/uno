#if XAMARIN
using System;
using System.Collections.Generic;
using System.Text;

#if XAMARIN_IOS
using View = UIKit.UIView;
#elif XAMARIN_ANDROID
using View = Android.Views.View;
#elif __MACOS__
using View = AppKit.NSView;
#endif

namespace Uno.UI.Controls
{
	/// <summary>
	/// Provides access to the shadowed children list for a native view
	/// inheriting control. Used to improve the children enumeration performance.
	/// </summary>
	internal interface IShadowChildrenProvider
	{
        /// <summary>
        /// An enumerable of children views.
        /// </summary>
        IReadOnlyList<View> ChildrenShadow { get; }
	}
}
#endif
