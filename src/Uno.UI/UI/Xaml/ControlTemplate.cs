using System;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif __MACOS__
using View = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ControlTemplate : FrameworkTemplate
	{
		public ControlTemplate() : this(null) { }

		public ControlTemplate (Func<View> factory)
			: base (factory)
		{
		}

		public static implicit operator ControlTemplate (Func<View> obj)
		{
			return new ControlTemplate (obj);
		}

		public Type TargetType { 
			get;
			set;
		}
	}
}

