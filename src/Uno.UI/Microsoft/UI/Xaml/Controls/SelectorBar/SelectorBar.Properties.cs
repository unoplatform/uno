// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Generated\SelectorBar.properties.cpp, tag winui3/release/1.5.2, commit b91b3ce6f25c587a9e18c4e122f348f51331f18b

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

partial class SelectorBar
{
	/// <summary>
	/// Gets the collection of SelectorBarItem instances used to generate the content of the SelectorBar control.
	/// </summary>
	public IList<SelectorBarItem> Items => (IList<SelectorBarItem>)GetValue(ItemsProperty);

	/// <summary>
	/// Identifies the Items dependency property.
	/// </summary>
	public static DependencyProperty ItemsProperty { get; } =
		DependencyProperty.Register(
			nameof(Items),
			typeof(IList<SelectorBarItem>),
			typeof(SelectorBar),
			new FrameworkPropertyMetadata(default(IList<SelectorBarItem>), OnPropertyChanged));

	/// <summary>
	/// Gets or sets the currently selected item.
	/// </summary>
	public SelectorBarItem SelectedItem
	{
		get => (SelectorBarItem)GetValue(SelectedItemProperty);
		set => SetValue(SelectedItemProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedItem dependency property.
	/// </summary>
	public static DependencyProperty SelectedItemProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedItem),
			typeof(SelectorBarItem),
			typeof(SelectorBar),
			new FrameworkPropertyMetadata(default(SelectorBarItem), OnPropertyChanged));

	/// <summary>
	/// Occurs when the SelectorBar selection changes; that is, when the SelectedItem property has changed.
	/// </summary>
	public event TypedEventHandler<SelectorBar, SelectorBarSelectionChangedEventArgs> SelectionChanged;

	private static void OnPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (SelectorBar)sender;
		owner.OnPropertyChanged(args);
	}
}
