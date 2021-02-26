using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif XAMARIN_IOS_UNIFIED
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif __MACOS__
using AppKit;
using View = System.Object;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
#else
using View = System.Object;
#endif

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// An interface consumed by <see cref="ScrollViewer"/>, which may contain either a <see cref="ScrollContentPresenter"/> (the
	/// normal case) or a <see cref="ListViewBaseScrollContentPresenter"/> (special case to handle usage within the template of 
	/// <see cref="ListViewBase"/>)
	/// </summary>
	internal partial interface IScrollContentPresenter
	{
		ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
		ScrollBarVisibility VerticalScrollBarVisibility { get; set; }
		View Content { get; set; }

		void OnMinZoomFactorChanged(float newValue);
		void OnMaxZoomFactorChanged(float newValue);

		Rect MakeVisible(UIElement visual, Rect rectangle);
	}
}
