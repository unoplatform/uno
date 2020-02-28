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
	}
}
