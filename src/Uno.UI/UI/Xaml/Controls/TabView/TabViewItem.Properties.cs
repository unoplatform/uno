// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabViewItem.Properties.cpp, commit 65718e2813

using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public partial class TabViewItem
{
	/// <summary>
	/// Gets or sets the content that appears inside the tab strip to represent the tab.
	/// </summary>
	public object Header
	{
		get => (object)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	/// <summary>
	/// Identifies the Header dependency property.
	/// </summary>
	public static DependencyProperty HeaderProperty { get; } =
		DependencyProperty.Register(nameof(Header), typeof(object), typeof(TabViewItem), new FrameworkPropertyMetadata(null, OnHeaderPropertyChanged));

	/// <summary>
	/// Gets or sets the DataTemplate used to display the content to the right of the tab strip.
	/// </summary>
	public DataTemplate HeaderTemplate
	{
		get => (DataTemplate)GetValue(HeaderTemplateProperty);
		set => SetValue(HeaderTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the HeaderTemplate dependency property.
	/// </summary>
	public static DependencyProperty HeaderTemplateProperty { get; } =
		DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(TabViewItem), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Gets or sets the value for the IconSource to be displayed within the tab.
	/// </summary>
	public IconSource IconSource
	{
		get => (IconSource)GetValue(IconSourceProperty);
		set => SetValue(IconSourceProperty, value);
	}

	/// <summary>
	/// Identifies the IconSource dependency property.
	/// </summary>
	public static DependencyProperty IconSourceProperty { get; } =
		DependencyProperty.Register(nameof(IconSource), typeof(IconSource), typeof(TabViewItem), new FrameworkPropertyMetadata(null, OnIconSourcePropertyChanged));

	/// <summary>
	/// Gets or sets the value that determines if the tab shows a close button. The default is true.
	/// </summary>
	public bool IsClosable
	{
		get => (bool)GetValue(IsClosableProperty);
		set => SetValue(IsClosableProperty, value);
	}

	/// <summary>
	/// Identifies the IsClosable dependency property.
	/// </summary>
	public static DependencyProperty IsClosableProperty { get; } =
		DependencyProperty.Register(nameof(IsClosable), typeof(bool), typeof(TabViewItem), new FrameworkPropertyMetadata(true, OnIsClosablePropertyChanged));

	/// <summary>
	/// Gets an object that provides calculated values that can be referenced as {TemplateBinding} markup extension
	/// sources when defining templates for a TabViewItem control.
	/// </summary>
	public TabViewItemTemplateSettings TabViewTemplateSettings =>
		(TabViewItemTemplateSettings)GetValue(TabViewTemplateSettingsProperty);

	/// <summary>
	/// Identifies the TabViewTemplateSettings dependency property.
	/// </summary>
	public static DependencyProperty TabViewTemplateSettingsProperty { get; } =
		DependencyProperty.Register(nameof(TabViewTemplateSettings), typeof(TabViewItemTemplateSettings), typeof(TabViewItem), new FrameworkPropertyMetadata(null));

	internal bool IsBeingDragged => m_isBeingDragged;

	private static void OnHeaderPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (TabViewItem)sender;
		owner.OnHeaderPropertyChanged(args);
	}

	private static void OnIconSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (TabViewItem)sender;
		owner.OnIconSourcePropertyChanged(args);
	}

	private static void OnIsClosablePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (TabViewItem)sender;
		owner.OnIsClosablePropertyChanged(args);
	}

	/// <summary>
	/// Raised when the user attempts to close the TabViewItem via clicking
	/// the x-to-close button, CTRL+F4, or mousewheel.
	/// </summary>
	public event TypedEventHandler<TabViewItem, TabViewTabCloseRequestedEventArgs> CloseRequested;
}
