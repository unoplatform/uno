using Uno;
using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class AppBarToggleButtonTemplateSettings : DependencyObject, IAppBarButtonTemplateSettings
{
	public double KeyboardAcceleratorTextMinWidth
	{
		get => (double)GetValue(KeyboardAcceleratorTextMinWidthProperty);
		internal set => SetValue(KeyboardAcceleratorTextMinWidthProperty, value);
	}

	internal static DependencyProperty KeyboardAcceleratorTextMinWidthProperty { get; } =
		DependencyProperty.Register(
			nameof(KeyboardAcceleratorTextMinWidth),
			typeof(double),
			typeof(AppBarToggleButtonTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	double IAppBarButtonTemplateSettings.KeyboardAcceleratorTextMinWidth
	{
		get => KeyboardAcceleratorTextMinWidth;
		set => KeyboardAcceleratorTextMinWidth = value;
	}
}
