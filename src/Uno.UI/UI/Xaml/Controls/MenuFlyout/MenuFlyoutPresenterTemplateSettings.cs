namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a MenuFlyoutPresenter control.
/// Not intended for general use.
/// </summary>
public partial class MenuFlyoutPresenterTemplateSettings : DependencyObject
{
	/// <summary>
	/// Gets the minimum width of flyout content.
	/// </summary>
	public double FlyoutContentMinWidth { get; internal set; }
}
