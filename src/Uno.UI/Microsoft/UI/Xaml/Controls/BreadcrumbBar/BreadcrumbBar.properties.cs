// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbBar.properties.cpp, tag winui3/release/1.5.3, commit 2a60e27

#nullable enable

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class BreadcrumbBar : Control
{
	/// <summary>
	/// Gets or sets an object source used to generate the content of the BreadcrumbBar.
	/// </summary>
	public object? ItemsSource
	{
		get => GetValue(ItemsSourceProperty);
		set => SetValue(ItemsSourceProperty, value);
	}

	/// <summary>
	/// Identifies the ItemsSource dependency property.
	/// </summary>
	public static DependencyProperty ItemsSourceProperty { get; } =
		DependencyProperty.Register(
			nameof(ItemsSource),
			typeof(object),
			typeof(BreadcrumbBar),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the data template for the BreadcrumbBarItem.
	/// </summary>
	public object? ItemTemplate
	{
		get => GetValue(ItemTemplateProperty);
		set => SetValue(ItemTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the ItemTemplate dependency property.
	/// </summary>
	public static DependencyProperty ItemTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(ItemTemplate),
			typeof(object),
			typeof(BreadcrumbBar),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (BreadcrumbBar)sender;
		owner.OnPropertyChanged(args);
	}

	/// <summary>
	/// Occurs when an item is clicked in the BreadcrumbBar.
	/// </summary>
	public event TypedEventHandler<BreadcrumbBar, BreadcrumbBarItemClickedEventArgs>? ItemClicked;
}
