using Windows.UI;
using Windows.UI.Xaml;

namespace Uno.UI.Toolkit
{
	public partial class ElevatedView
	{
		public static readonly DependencyProperty ElevationProperty = DependencyProperty.Register(
			"Elevation", typeof(double), typeof(ElevatedView), new PropertyMetadata(default(double), OnChanged));

#if __ANDROID__
		public new double Elevation
#else
		public double Elevation
#endif
		{
			get => (double)GetValue(ElevationProperty);
			set => SetValue(ElevationProperty, value);
		}

		public static readonly DependencyProperty ShadowColorProperty = DependencyProperty.Register(
			"ShadowColor", typeof(Color), typeof(ElevatedView), new PropertyMetadata(Color.FromArgb(64, 0, 0, 0), OnChanged));

		public Color ShadowColor
		{
			get => (Color)GetValue(ShadowColorProperty);
			set => SetValue(ShadowColorProperty, value);
		}
	}
}
