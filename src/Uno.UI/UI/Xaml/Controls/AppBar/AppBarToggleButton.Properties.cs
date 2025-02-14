using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

//TODO:MZ: Validate getter/setter overrides!

partial class AppBarToggleButton
{
	/// <summary>
	/// Gets or sets the order in which this item is moved to the CommandBar overflow menu.
	/// </summary>
	public int DynamicOverflowOrder
	{
		get => (int)GetValue(DynamicOverflowOrderProperty);
		set => SetValue(DynamicOverflowOrderProperty, value);
	}

	/// <summary>
	/// Identifies the DynamicOverflowOrder dependency property.
	/// </summary>
	public static DependencyProperty DynamicOverflowOrderProperty { get; } =
		DependencyProperty.Register(
			nameof(DynamicOverflowOrder),
			typeof(int),
			typeof(AppBarToggleButton),
			new FrameworkPropertyMetadata(default(int)));

	/// <summary>
	/// Gets or sets the graphic content of the app bar toggle button.
	/// </summary>
	public IconElement Icon
	{
		get => (IconElement)GetValue(IconProperty);
		set => SetValue(IconProperty, value);
	}

	/// <summary>
	/// Identifies the Icon dependency property.
	/// </summary>
	public static DependencyProperty IconProperty { get; } =
		DependencyProperty.Register(
			nameof(Icon),
			typeof(IconElement),
			typeof(AppBarToggleButton),
			new FrameworkPropertyMetadata(default(IconElement)));

	/// <summary>
	/// Gets or sets a value that indicates whether the button is shown with no label and reduced padding.
	/// </summary>
	public bool IsCompact
	{
		get => (bool)GetValue(IsCompactProperty);
		set => SetValue(IsCompactProperty, value);
	}

	/// <summary>
	/// Identifies the IsCompact dependency property.
	/// </summary>
	public static DependencyProperty IsCompactProperty { get; } =
		DependencyProperty.Register(
			nameof(IsCompact),
			typeof(bool),
			typeof(AppBarToggleButton),
			new FrameworkPropertyMetadata(default(bool))
		);

	/// <summary>
	/// Gets a value that indicates whether this item is in the overflow menu.
	/// </summary>
	public bool IsInOverflow
	{
		get => CommandBar.IsCommandBarElementInOverflow(this);
		private set => SetValue(IsInOverflowProperty, value);
	}

	/// <summary>
	/// Identifies the IsInOverflow dependency property.
	/// </summary>
	public static DependencyProperty IsInOverflowProperty { get; } =
		DependencyProperty.Register(
			nameof(IsInOverflow),
			typeof(bool),
			typeof(AppBarToggleButton),
			new FrameworkPropertyMetadata(false));

	// TODO:MZ: Does the getter/setter only apply when retrieved via the public property or also via DP GetValue/SetValue?
	/// <summary>
	/// Gets or sets a string that overrides the default key combination string associated with a keyboard accelerator.
	/// </summary>
	public string KeyboardAcceleratorTextOverride
	{
		get => AppBarButtonHelpers<AppBarToggleButton>.GetKeyboardAcceleratorText(this);
		set => AppBarButtonHelpers<AppBarToggleButton>.PutKeyboardAcceleratorText(this, value);
	}

	/// <summary>
	/// Identifies the AppBarToggleButton.KeyboardAcceleratorTextOverride dependency property.
	/// </summary>
	public static DependencyProperty KeyboardAcceleratorTextOverrideProperty { get; } =
		DependencyProperty.Register(
			nameof(KeyboardAcceleratorTextOverride),
			typeof(string),
			typeof(AppBarToggleButton),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Gets or sets a value that indicates the placement and visibility of the button's label.
	/// </summary>
	public CommandBarLabelPosition LabelPosition
	{
		get => (CommandBarLabelPosition)GetValue(LabelPositionProperty);
		set => SetValue(LabelPositionProperty, value);
	}

	/// <summary>
	/// Identifies the LabelPosition dependency property.
	/// </summary>
	public static DependencyProperty LabelPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(LabelPosition),
			typeof(CommandBarLabelPosition),
			typeof(AppBarToggleButton),
			new FrameworkPropertyMetadata(default(CommandBarLabelPosition)));

	/// <summary>
	/// Gets or sets the text description displayed on the app bar toggle button.
	/// </summary>
	public string Label
	{
		get => (string)GetValue(LabelProperty); // TODO:MZ: Should retur null or empty string? Validate against code in GetEffectiveLabelPosition!
		set => SetValue(LabelProperty, value);
	}

	/// <summary>
	/// Identifies the Label dependency property.
	/// </summary>
	public static DependencyProperty LabelProperty { get; } =
		DependencyProperty.Register(
			nameof(Label),
			typeof(string),
			typeof(AppBarToggleButton),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Gets an object that provides calculated values that can be referenced as {TemplateBinding} markup extension
	/// sources when defining templates for an AppBarToggleButton control.
	/// </summary>
	public AppBarToggleButtonTemplateSettings TemplateSettings
	{
		get => (AppBarToggleButtonTemplateSettings)GetValue(TemplateSettingsProperty);
		private set => SetValue(TemplateSettingsProperty, value);
	}

	private static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(
			nameof(TemplateSettings),
			typeof(AppBarToggleButtonTemplateSettings),
			typeof(AppBarToggleButton),
			new FrameworkPropertyMetadata(null));

	internal bool UseOverflowStyle
	{
		get => (bool)GetValue(UseOverflowStyleProperty);
		set => SetValue(UseOverflowStyleProperty, value);
	}

	bool ICommandBarOverflowElement.UseOverflowStyle
	{
		get => UseOverflowStyle;
		set => UseOverflowStyle = value;
	}

	internal static DependencyProperty UseOverflowStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(UseOverflowStyle),
			typeof(bool),
			typeof(AppBarToggleButton),
			new FrameworkPropertyMetadata(default(bool)));

	bool ICommandBarElement3.IsInOverflow
	{
		get => IsInOverflow;
		set => IsInOverflow = value;
	}
}
