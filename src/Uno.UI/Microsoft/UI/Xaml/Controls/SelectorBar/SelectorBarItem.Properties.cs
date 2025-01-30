// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Generated\SelectorBarItem.properties.cpp, tag winui3/release/1.5.2, commit b91b3ce6f25c587a9e18c4e122f348f51331f18b

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls;

partial class SelectorBarItem
{
	/// <summary>
	/// Gets or sets the item's graphical icon.
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
			typeof(SelectorBarItem),
			new FrameworkPropertyMetadata(default(IconElement), OnPropertyChanged));

	/// <summary>
	/// Gets or sets the item's text label.
	/// </summary>
	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	/// <summary>
	/// Identifies the Text dependency property.
	/// </summary>
	public static DependencyProperty TextProperty { get; } =
		DependencyProperty.Register(
			nameof(Text),
			typeof(string),
			typeof(SelectorBarItem),
			new FrameworkPropertyMetadata("", OnPropertyChanged));

	private static void OnPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (SelectorBarItem)sender;
		owner.OnPropertyChanged(args);
	}
}
