// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\MenuFlyoutItem_Partial.cpp, tag winui3/release/1.8.1, commit cd3b7ad0eca

using System.Windows.Input;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyoutItem : MenuFlyoutItemBase
{
	/// <summary>
	/// Gets or sets the command to invoke when the item is pressed.
	/// </summary>
	public ICommand Command
	{
		get => (ICommand)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
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
		get => (object)GetValue(CommandParameterProperty);
		set => SetValue(CommandParameterProperty, value);
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
		get
		{
			InitializeKeyboardAcceleratorText();
			return (string)this.GetValue(KeyboardAcceleratorTextOverrideProperty) ?? "";
		}

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
		get => (string)GetValue(TextProperty) ?? "";
		set => SetValue(TextProperty, value);
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

	internal bool PreventDismissOnPointer { get; set; }

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
	/// <summary>
	/// Occurs when a menu item is clicked.
	/// </summary>
	public event RoutedEventHandler Click;
#pragma warning restore CS0108
}
