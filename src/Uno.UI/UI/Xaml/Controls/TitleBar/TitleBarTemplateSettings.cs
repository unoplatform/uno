namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a TitleBar.
/// </summary>
public partial class TitleBarTemplateSettings : DependencyObject
{
	/// <summary>
	/// Initializes a new instance of the TitleBarTemplateSettings class.
	/// </summary>
	public TitleBarTemplateSettings()
	{

	}

	/// <summary>
	/// Gets or sets the icon for the title bar.
	/// </summary>
	public IconElement IconElement
	{
		get => (IconElement)this.GetValue(IconElementProperty);
		set => SetValue(IconElementProperty, value);
	}

	/// <summary>
	/// Identifies the IconElement dependency property.
	/// </summary>
	public static DependencyProperty IconElementProperty { get; } =
		DependencyProperty.Register(
			nameof(IconElement),
			typeof(IconElement),
			typeof(TitleBarTemplateSettings),
			new FrameworkPropertyMetadata(default(IconElement)));

}
