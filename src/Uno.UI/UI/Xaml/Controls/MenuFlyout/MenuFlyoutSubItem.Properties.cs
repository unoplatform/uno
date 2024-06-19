using System;
using System.Collections.Generic;
using Uno.Disposables;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;

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
	public IList<MenuFlyoutItemBase> Items => m_tpItems;

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
