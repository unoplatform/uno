// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\winrtgeneratedclasses\SplitMenuFlyoutItem.g.cpp, commit 5f9e85113

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

partial class SplitMenuFlyoutItem
{
	/// <summary>
	/// Gets the collection of menu items to display in the submenu.
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
			typeof(SplitMenuFlyoutItem),
			new FrameworkPropertyMetadata(defaultValue: null));

	/// <summary>
	/// Gets or sets the style applied to the submenu's flyout presenter.
	/// </summary>
	public Style SubMenuPresenterStyle
	{
		get => (Style)this.GetValue(SubMenuPresenterStyleProperty);
		set => this.SetValue(SubMenuPresenterStyleProperty, value);
	}

	/// <summary>
	/// Identifies the SubMenuPresenterStyle dependency property.
	/// </summary>
	public static DependencyProperty SubMenuPresenterStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(SubMenuPresenterStyle),
			typeof(Style),
			typeof(SplitMenuFlyoutItem),
			new FrameworkPropertyMetadata(
				default(Style),
				(s, e) => (s as SplitMenuFlyoutItem)?.OnSubMenuPresenterStyleChanged(e)));

	/// <summary>
	/// Gets or sets the style applied to individual items within the submenu.
	/// </summary>
	public Style SubMenuItemStyle
	{
		get => (Style)this.GetValue(SubMenuItemStyleProperty);
		set => this.SetValue(SubMenuItemStyleProperty, value);
	}

	/// <summary>
	/// Identifies the SubMenuItemStyle dependency property.
	/// </summary>
	public static DependencyProperty SubMenuItemStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(SubMenuItemStyle),
			typeof(Style),
			typeof(SplitMenuFlyoutItem),
			new FrameworkPropertyMetadata(
				default(Style),
				(s, e) => (s as SplitMenuFlyoutItem)?.OnSubMenuItemStyleChanged(e)));
}
