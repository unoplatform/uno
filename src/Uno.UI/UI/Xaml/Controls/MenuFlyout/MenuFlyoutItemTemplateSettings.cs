namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a MenuFlyoutPresenter control.
/// Not intended for general use.
/// </summary>
public partial class MenuFlyoutItemTemplateSettings : DependencyObject
{
	/// <summary>
	/// Gets the minimum width allocated for the accelerator key tip of an MenuFlyout.
	/// </summary>
	public double KeyboardAcceleratorTextMinWidth { get; internal set; }
}
