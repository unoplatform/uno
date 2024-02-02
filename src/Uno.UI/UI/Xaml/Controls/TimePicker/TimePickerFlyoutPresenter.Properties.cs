#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class TimePickerFlyoutPresenter
{
	/// <summary>
	/// Gets or sets a value that indicates whether the default shadow effect is shown.
	/// </summary>
	public new bool IsDefaultShadowEnabled
	{
		get => (bool)GetValue(IsDefaultShadowEnabledProperty);
		set => SetValue(IsDefaultShadowEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsDefaultShadowEnabled dependency property.
	/// </summary>
	public static new DependencyProperty IsDefaultShadowEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsDefaultShadowEnabled),
			typeof(bool),
			typeof(TimePickerFlyoutPresenter),
			new FrameworkPropertyMetadata(TimePickerFlyoutPresenter.GetDefaultIsDefaultShadowEnabled()));
}
