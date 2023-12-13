namespace Windows.UI.Xaml.Controls;

partial class FlyoutPresenter
{
	/// <summary>
	/// Gets or sets a value that indicates whether the default shadow effect is shown.
	/// </summary>
	public bool IsDefaultShadowEnabled
	{
		get => (bool)GetValue(IsDefaultShadowEnabledProperty);
		set => SetValue(IsDefaultShadowEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsDefaultShadowEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsDefaultShadowEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsDefaultShadowEnabled),
			typeof(bool),
			typeof(FlyoutPresenter),
			new FrameworkPropertyMetadata(default(bool)));
}
