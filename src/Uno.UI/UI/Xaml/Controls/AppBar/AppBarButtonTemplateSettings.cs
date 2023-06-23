
using Uno;
using Uno.UI.Helpers;
using Windows.UI.Xaml;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class AppBarButtonTemplateSettings : DependencyObject
	{
		public double KeyboardAcceleratorTextMinWidth
		{
			get => (double)GetValue(KeyboardAcceleratorTextMinWidthProperty);
			internal set => SetValue(KeyboardAcceleratorTextMinWidthProperty, value);
		}

		internal static DependencyProperty KeyboardAcceleratorTextMinWidthProperty { get; } =
			DependencyProperty.Register("KeyboardAcceleratorTextMinWidth", typeof(double), typeof(AppBarButtonTemplateSettings), new FrameworkPropertyMetadata(Boxes.DoubleBoxes.Zero));
	}
}
