using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif XAMARIN_IOS
using UIKit;
using View = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
#else
using System.Drawing;
#endif

namespace Windows.UI.Xaml.Media
{
	public partial class SolidColorBrush : Brush
	{
		/// <summary>
		/// Blends the Color set on the SolidColorBrush with its Opacity. Should generally be used for rendering rather than the Color property itself.
		/// </summary>
		internal Color ColorWithOpacity
		{
			get; set;
		}

		/// <remarks>
		/// This method is required for performance. Creating a native Color 
		/// requires a round-trip with Objective-C, so updating this value only when opacity
		/// and color changes is more efficient.
		/// </remarks>
		partial void UpdateColorWithOpacity(Color newColor, double opacity)
		{
			ColorWithOpacity = GetColorWithOpacity(newColor);
		}
	}
}
