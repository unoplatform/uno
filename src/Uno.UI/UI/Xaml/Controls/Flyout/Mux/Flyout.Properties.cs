namespace Windows.UI.Xaml.Controls;

partial class Flyout
{
	/// <summary>
	/// Gets or sets the content of the Flyout.
	/// </summary>
	public UIElement Content
	{
		get => (UIElement)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the Content dependency property.
	/// </summary>
	public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(
			nameof(Content),
			typeof(UIElement),
			typeof(Flyout),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the Style applied to the Flyout content.
	/// </summary>
	public Style FlyoutPresenterStyle
	{
		get => (Style)GetValue(FlyoutPresenterStyleProperty);
		set => SetValue(FlyoutPresenterStyleProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the FlyoutPresenterStyle dependency property.
	/// </summary>
	public static DependencyProperty FlyoutPresenterStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(FlyoutPresenterStyle),
			typeof(Style),
			typeof(Flyout),
			new FrameworkPropertyMetadata(null));
}
