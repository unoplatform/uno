using System;
using Microsoft.UI.Xaml.Markup;

#if __ANDROID__
using Android.Views;
using Android.Graphics;
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif __IOS__
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using UIKit;
using Windows.UI;
#elif __MACOS__
using Color = Windows.UI.Color;
#else
using Windows.UI;
#endif

namespace Microsoft.UI.Xaml.Media
{
	[ContentProperty(Name = nameof(Color))]
	public partial class GradientStop : DependencyObject
	{
		public GradientStop()
		{
			InitializeBinder();
		}

		internal event Action InvalidateRender;

		public Windows.UI.Color Color
		{
			get { return (Windows.UI.Color)this.GetValue(ColorProperty); }
			set { this.SetValue(ColorProperty, value); }
		}
		public static DependencyProperty ColorProperty { get; } =
			DependencyProperty.Register(
				"Color",
				typeof(Windows.UI.Color),
				typeof(GradientStop),
				new FrameworkPropertyMetadata(Colors.Transparent, propertyChangedCallback: (s, _) => ((GradientStop)s).InvalidateRender?.Invoke())
			);

		public double Offset
		{
			get { return (double)this.GetValue(OffsetProperty); }
			set { this.SetValue(OffsetProperty, value); }
		}
		public static DependencyProperty OffsetProperty { get; } =
			DependencyProperty.Register(
				"Offset",
				typeof(double),
				typeof(GradientStop),
				new FrameworkPropertyMetadata(default(double), propertyChangedCallback: (s, _) => ((GradientStop)s).InvalidateRender?.Invoke())
			);
	}
}
