using System;
using Windows.UI.Xaml.Markup;
using Windows.UI;

namespace Windows.UI.Xaml.Media
{
	[ContentProperty(Name = nameof(Color))]
	public partial class GradientStop : DependencyObject
	{
		public GradientStop()
		{
			InitializeBinder();
		}

		internal event Action InvalidateRender;

		public Color Color
		{
			get { return (Color)this.GetValue(ColorProperty); }
			set { this.SetValue(ColorProperty, value); }
		}
		public static DependencyProperty ColorProperty { get; } =
			DependencyProperty.Register(
				"Color",
				typeof(Color),
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
