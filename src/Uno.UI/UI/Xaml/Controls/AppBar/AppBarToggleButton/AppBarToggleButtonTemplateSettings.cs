using Uno;
using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for an AppBarToggleButton control.
/// Not intended for general use.
/// </summary>
public partial class AppBarToggleButtonTemplateSettings : DependencyObject, IAppBarButtonTemplateSettings
{
	/// <summary>
	/// Gets the minimum width allocated for the accelerator key tip of an AppBarToggleButton.
	/// </summary>
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
