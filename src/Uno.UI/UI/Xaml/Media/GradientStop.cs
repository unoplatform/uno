#if XAMARIN_ANDROID
using Android.Views;
using Android.Graphics;
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using UIKit;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
using MonoTouch.UIKit;
#elif __MACOS__
using Color = Windows.UI.Color;
#else
using Color = System.Drawing.Color;
#endif

namespace Windows.UI.Xaml.Media
{
	public partial class GradientStop : DependencyObject
	{
		public GradientStop()
		{
			InitializeBinder();
		}

		public Color Color
		{
			get { return (Color)this.GetValue(ColorProperty); }
			set { this.SetValue(ColorProperty, value); }
		}
		public static readonly DependencyProperty ColorProperty =
			DependencyProperty.Register(
				"Color",
				typeof(Color),
				typeof(GradientStop),
				new PropertyMetadata(Colors.Transparent)
			);

		public double Offset
		{
			get { return (double)this.GetValue(OffsetProperty); }
			set { this.SetValue(OffsetProperty, value); }
		}
		public static readonly DependencyProperty OffsetProperty =
			DependencyProperty.Register(
				"Offset",
				typeof(double),
				typeof(GradientStop),
				new PropertyMetadata(default(double))
			);
	}
}
