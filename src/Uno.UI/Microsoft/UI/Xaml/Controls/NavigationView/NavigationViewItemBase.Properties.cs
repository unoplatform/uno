// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewItemBase.properties.cpp, commit de78834

using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class NavigationViewItemBase
{
	/// <summary>
	/// Gets or sets the value that indicates
	/// whether a NavigationViewItem is selected.
	/// </summary>
	public bool IsSelected
	{
		get => (bool)GetValue(IsSelectedProperty);
		set => SetValue(IsSelectedProperty, value);
	}

	/// <summary>
	/// Identifies the IsSelected dependency property.
	/// </summary>
	public static DependencyProperty IsSelectedProperty { get; } =
		DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(NavigationViewItemBase), new FrameworkPropertyMetadata(false, OnIsSelectedPropertyChanged));

	private static void OnIsSelectedPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (NavigationViewItemBase)sender;
		owner.OnPropertyChanged(args);
	}
}
