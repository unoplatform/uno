using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class IconSource : DependencyObject
	{
		public Brush Foreground
		{
			get => (Brush)GetValue(ForegroundProperty);
			set => SetValue(ForegroundProperty, value);
		}

		public static DependencyProperty ForegroundProperty { get; } =
			DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(IconSource), new PropertyMetadata(null));
	}
}
