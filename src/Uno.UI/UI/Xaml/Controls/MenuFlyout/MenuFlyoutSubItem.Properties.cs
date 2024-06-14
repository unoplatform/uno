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
	public IconElement Icon
	{
		get => (IconElement)this.GetValue(IconProperty);
		set => this.SetValue(IconProperty, value);
	}


	public string Text
	{
		get => (string)this.GetValue(TextProperty) ?? "";
		set => SetValue(TextProperty, value);
	}

	public static Microsoft.UI.Xaml.DependencyProperty TextProperty { get; } =
	Microsoft.UI.Xaml.DependencyProperty.Register(
		"Text",
		typeof(string),
		typeof(MenuFlyoutSubItem),
		new FrameworkPropertyMetadata(default(string)));

	public static Microsoft.UI.Xaml.DependencyProperty IconProperty { get; } =
	Microsoft.UI.Xaml.DependencyProperty.Register(
		"Icon",
		typeof(IconElement),
		typeof(MenuFlyoutSubItem),
		new FrameworkPropertyMetadata(default(IconElement)));
}
