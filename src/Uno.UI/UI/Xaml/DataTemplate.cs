using System;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
using ViewGroup = MonoTouch.UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
using ViewGroup = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml
{
	public partial class DataTemplate : FrameworkTemplate
	{
		public DataTemplate (Func<View> factory)
			: base (factory)
		{
		}

		public static implicit operator DataTemplate(Func<View> obj)
		{
            if(obj == null)
            {
                return null;
            }

            return new DataTemplate(obj);
		}
	}
}

