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
		/// <remarks>
		/// This property is exposed as a concrete <see cref="List{T}"/> to benefit from
		/// allocation-less enumeration of the shadow children.
		/// </remarks>
        List<View> ChildrenShadow { get; }
	}
}
#endif
