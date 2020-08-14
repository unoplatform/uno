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
using Windows.UI;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
using MonoTouch.UIKit;
#elif __MACOS__
using Color = Windows.UI.Color;
#else
using Windows.UI;
#endif

namespace Windows.UI.Xaml.Media
{
	public partial class GradientStop : DependencyObject
	{
		public GradientStop()
		{
			InitializeBinder();
		}

		public Windows.UI.Color Color
		{
			get { return (Windows.UI.Color)this.GetValue(ColorProperty); }
			set { this.SetValue(ColorProperty, value); }
		}
		public static DependencyProperty ColorProperty { get ; } =
			DependencyProperty.Register(
				"Color",
				typeof(Windows.UI.Color),
				typeof(GradientStop),
				new FrameworkPropertyMetadata(Colors.Transparent)
			);

		public double Offset
		{
			get { return (double)this.GetValue(OffsetProperty); }
			set { this.SetValue(OffsetProperty, value); }
		}
		public static DependencyProperty OffsetProperty { get ; } =
			DependencyProperty.Register(
				"Offset",
				typeof(double),
				typeof(GradientStop),
				new FrameworkPropertyMetadata(default(double))
			);
	}
}
