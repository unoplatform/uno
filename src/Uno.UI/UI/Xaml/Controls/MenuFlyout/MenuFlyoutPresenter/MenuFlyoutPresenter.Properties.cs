using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyoutPresenter
{
	/// <summary>
	/// Gets or sets a value that indicates whether the default shadow effect is shown.
	/// </summary>
	public bool IsDefaultShadowEnabled
	{
		get => (bool)this.GetValue(IsDefaultShadowEnabledProperty);
		set => this.SetValue(IsDefaultShadowEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsDefaultShadowEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsDefaultShadowEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsDefaultShadowEnabled),
			typeof(bool),
			typeof(MenuFlyoutPresenter),
			new FrameworkPropertyMetadata(defaultValue: true));

	/// <summary>
	/// Gets an object that provides calculated values that can be referenced
	/// as TemplateBinding sourceswhen defining templates for a MenuFlyoutPresenter control.
	/// </summary>
	public MenuFlyoutPresenterTemplateSettings TemplateSettings { get; private set; }
}
