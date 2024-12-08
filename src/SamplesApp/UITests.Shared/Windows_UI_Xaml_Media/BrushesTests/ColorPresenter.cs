using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UITests.Windows_UI_Xaml_Media.BrushesTests
{
	public partial class ColorPresenter : Panel
	{
		public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
			"Color", typeof(Color), typeof(ColorPresenter), new PropertyMetadata(default(Color), OnColorChanged));

		private static void OnColorChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyobject is ColorPresenter cp)
			{
				cp.Background = new SolidColorBrush((Windows.UI.Color)args.NewValue);
			}
		}

		public Color Color
		{
			get => (Color)GetValue(ColorProperty);
			set => SetValue(ColorProperty, value);
		}
	}
}
