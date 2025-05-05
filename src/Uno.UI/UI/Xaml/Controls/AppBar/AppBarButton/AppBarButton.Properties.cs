#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBarButton
{
	/// <summary>
	/// Represents a templated button control to be displayed in an AppBar.
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
			typeof(AppBarButton),
			new FrameworkPropertyMetadata(default(int)));

	/// <summary>
	/// Gets or sets the image displayed on the app bar button.
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
			typeof(AppBarButton),
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
			typeof(AppBarButton),
			new FrameworkPropertyMetadata(default(bool)));

	/// <summary>
	/// Gets a value that indicates whether this item is in the overflow menu.
	/// </summary>
	public bool IsInOverflow
	{
		get => CommandBar.IsCommandBarElementInOverflow(this);
		internal set => SetValue(IsInOverflowProperty, value);
	}

	/// <summary>
	/// Identifies the IsInOverflow dependency property.
	/// </summary>
	public static DependencyProperty IsInOverflowProperty { get; } =
		DependencyProperty.Register(
			nameof(IsInOverflow),
			typeof(bool),
			typeof(AppBarButton),
			new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets a string that overrides the default key combination string associated with a keyboard accelerator.
	/// </summary>
	public string KeyboardAcceleratorTextOverride
	{
		get => AppBarButtonHelpers<AppBarButton>.GetKeyboardAcceleratorText(this);
		set => AppBarButtonHelpers<AppBarButton>.PutKeyboardAcceleratorText(this, value);
	}

	/// <summary>
	/// Identifies the AppBarButton.KeyboardAcceleratorTextOverride dependency property.
	/// </summary>
	public static DependencyProperty KeyboardAcceleratorTextOverrideProperty { get; } =
		DependencyProperty.Register(
			nameof(KeyboardAcceleratorTextOverride),
			typeof(string),
			typeof(AppBarButton),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Gets or sets the text displayed on the app bar button.
	/// </summary>
	public string Label
	{
		get => (string)GetValue(LabelProperty);
		set => SetValue(LabelProperty, value);
	}

	/// <summary>
	/// Identifies the Label dependency property.
	/// </summary>
	public static DependencyProperty LabelProperty { get; } =
		DependencyProperty.Register(
			nameof(Label),
			typeof(string),
			typeof(AppBarButton),
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
			typeof(AppBarButton),
			new FrameworkPropertyMetadata(default(CommandBarLabelPosition))
		);

	/// <summary>
	/// Gets an object that provides calculated values that can be referenced as {TemplateBinding} markup extension sources when defining templates for an AppBarButton control.
	/// </summary>
	public AppBarButtonTemplateSettings TemplateSettings
	{
		get => (AppBarButtonTemplateSettings)GetValue(TemplateSettingsProperty);
		private set => SetValue(TemplateSettingsProperty, value);
	}

	internal static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(
			nameof(TemplateSettings),
			typeof(AppBarButtonTemplateSettings),
			typeof(AppBarButton),
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
			typeof(AppBarButton),
			new FrameworkPropertyMetadata(default(bool)));

	bool ICommandBarElement3.IsInOverflow
	{
		get => IsInOverflow;
		set => IsInOverflow = value;
	}
}
