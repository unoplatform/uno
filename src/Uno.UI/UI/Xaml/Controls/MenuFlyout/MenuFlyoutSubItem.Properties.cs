// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\MenuFlyoutSubItem_Partial.cpp, tag winui3/release/1.5.4, commit 98a60c8

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyoutSubItem
{
	/// <summary>
	/// Gets or sets the graphic content of the menu flyout subitem.
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
			nameof(Icon),
			typeof(IconElement),
			typeof(MenuFlyoutSubItem),
			new FrameworkPropertyMetadata(default(IconElement)));

	/// <summary>
	/// Gets the collection used to generate the content of the sub-menu.
	/// </summary>
	public IList<MenuFlyoutItemBase> Items
	{
		get => (IList<MenuFlyoutItemBase>)this.GetValue(ItemsProperty);
		private set => this.SetValue(ItemsProperty, value);
	}

	/// <summary>
	/// Identifies the Items dependency property.
	/// </summary>
	internal static DependencyProperty ItemsProperty { get; } =
		DependencyProperty.Register(
			nameof(Items),
			typeof(IList<MenuFlyoutItemBase>),
			typeof(MenuFlyoutSubItem),
			new FrameworkPropertyMetadata(defaultValue: null));

	/// <summary>
	/// Gets or sets the text content of a MenuFlyoutSubItem.
	/// </summary>
	public string Text
	{
		get => (string)this.GetValue(TextProperty) ?? "";
		set => SetValue(TextProperty, value);
	}

	/// <summary>
	/// Identifies the Text dependency property.
	/// </summary>
	public static DependencyProperty TextProperty { get; } =
		DependencyProperty.Register(
			nameof(Text),
			typeof(string),
			typeof(MenuFlyoutSubItem),
			new FrameworkPropertyMetadata(default(string)));
}
