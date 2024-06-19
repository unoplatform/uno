using System.Windows.Input;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyoutItem : MenuFlyoutItemBase
{
	/// <summary>
	/// Gets or sets the command to invoke when the item is pressed.
	/// </summary>
	public ICommand Command
	{
		get { return (ICommand)GetValue(CommandProperty); }
		set { SetValue(CommandProperty, value); }
	}

	/// <summary>
	/// Identifies the Command dependency property.
	/// </summary>
	public static DependencyProperty CommandProperty { get; } =
		DependencyProperty.Register(
			name: nameof(Command),
			propertyType: typeof(ICommand),
			ownerType: typeof(MenuFlyoutItem),
			typeMetadata: new FrameworkPropertyMetadata(default(ICommand)));

	/// <summary>
	/// Gets or sets the parameter to pass to the Command property.
	/// </summary>
	public object CommandParameter
	{
		get { return (object)GetValue(CommandParameterProperty); }
		set { SetValue(CommandParameterProperty, value); }
	}

	/// <summary>
	/// Identifies the CommandParameter dependency property.
	/// </summary>
	public static DependencyProperty CommandParameterProperty { get; } =
		DependencyProperty.Register(
			"CommandParameter", typeof(object),
			typeof(Controls.MenuFlyoutItem),
			new FrameworkPropertyMetadata(default(object)));

	/// <summary>
	/// Gets or sets the graphic content of the menu flyout item.
	/// </summary>
	public IconElement Icon
	{
		get => (IconElement)this.GetValue(IconProperty);
		set => this.SetValue(IconProperty, value);
	}

	/// <summary>
	/// Identifies the Icon dependency property.
	/// </summary>
	public static DependencyProperty IconProperty { get; } =
		DependencyProperty.Register(
			name: nameof(Icon),
			propertyType: typeof(IconElement),
			ownerType: typeof(MenuFlyoutItem),
			typeMetadata: new FrameworkPropertyMetadata(default(IconElement)));

	/// <summary>
	/// Gets or sets a string that overrides the default key combination string associated with a keyboard accelerator.
	/// </summary>
	public string KeyboardAcceleratorTextOverride
	{
		get => (string)this.GetValue(KeyboardAcceleratorTextOverrideProperty) ?? "";
		set => this.SetValue(KeyboardAcceleratorTextOverrideProperty, value);
	}

	/// <summary>
	/// Identifies the MenuFlyoutItem.KeyboardAcceleratorTextOverride dependency property.
	/// </summary>
	public static DependencyProperty KeyboardAcceleratorTextOverrideProperty { get; } =
		DependencyProperty.Register(
			name: nameof(KeyboardAcceleratorTextOverride),
			propertyType: typeof(string),
			ownerType: typeof(MenuFlyoutItem),
			typeMetadata: new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Gets an object that provides calculated values that can be referenced as {TemplateBinding} markup extension sources when defining templates for a MenuFlyoutItem control.
	/// </summary>
	public MenuFlyoutItemTemplateSettings TemplateSettings { get; internal set; }

	/// <summary>
	/// Gets or sets the text content of a MenuFlyoutItem.
	/// </summary>
	public string Text
	{
		get { return (string)GetValue(TextProperty) ?? ""; }
		set { SetValue(TextProperty, value); }
	}

	/// <summary>
	/// Identifies the Text dependency property.
	/// </summary>
	public static DependencyProperty TextProperty { get; } =
		DependencyProperty.Register(
			name: nameof(Text),
			propertyType: typeof(string),
			ownerType: typeof(MenuFlyoutItem),
			typeMetadata: new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Occurs when a menu item is clicked.
	/// </summary>
	public event RoutedEventHandler Click;
}
