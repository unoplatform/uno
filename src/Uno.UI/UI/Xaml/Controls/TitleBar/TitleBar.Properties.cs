// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class TitleBar
{
	/// <summary>
	/// Gets or sets the content to show in the center of the title bar.
	/// </summary>
	public UIElement Content
	{
		get => (UIElement)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	/// <summary>
	/// Identifies the Content dependency property.
	/// </summary>
	public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(nameof(Content), typeof(UIElement), typeof(TitleBar), new FrameworkPropertyMetadata(null, OnGenericPropertyChanged));

	/// <summary>
	/// Gets or sets the icon image to show in the title bar.
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
		DependencyProperty.Register(nameof(IconSource), typeof(IconSource), typeof(TitleBar), new FrameworkPropertyMetadata(null, OnGenericPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the button to navigate back is enabled.
	/// </summary>
	public bool IsBackButtonEnabled
	{
		get => (bool)GetValue(IsBackButtonEnabledProperty);
		set => SetValue(IsBackButtonEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsBackButtonEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsBackButtonEnabledProperty { get; } =
		DependencyProperty.Register(nameof(IsBackButtonEnabled), typeof(bool), typeof(TitleBar), new FrameworkPropertyMetadata(true, OnGenericPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the button to navigate back is shown.
	/// </summary>
	public bool IsBackButtonVisible
	{
		get => (bool)GetValue(IsBackButtonVisibleProperty);
		set => SetValue(IsBackButtonVisibleProperty, value);
	}

	/// <summary>
	/// Identifies the IsBackButtonVisible dependency property.
	/// </summary>
	public static DependencyProperty IsBackButtonVisibleProperty { get; } =
		DependencyProperty.Register(nameof(IsBackButtonVisible), typeof(bool), typeof(TitleBar), new FrameworkPropertyMetadata(false, OnGenericPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the button to toggle the navigation pane is shown.
	/// </summary>
	public bool IsPaneToggleButtonVisible
	{
		get => (bool)GetValue(IsPaneToggleButtonVisibleProperty);
		set => SetValue(IsPaneToggleButtonVisibleProperty, value);
	}

	/// <summary>
	/// Identifies the IsPaneToggleButtonVisible dependency property.
	/// </summary>
	public static DependencyProperty IsPaneToggleButtonVisibleProperty { get; } =
		DependencyProperty.Register(nameof(IsPaneToggleButtonVisible), typeof(bool), typeof(TitleBar), new FrameworkPropertyMetadata(false, OnGenericPropertyChanged));

	/// <summary>
	/// Gets or sets the content to show in the left side of the title bar.
	/// </summary>
	public UIElement LeftHeader
	{
		get => (UIElement)GetValue(LeftHeaderProperty);
		set => SetValue(LeftHeaderProperty, value);
	}

	/// <summary>
	/// Identifies the LeftHeader dependency property.
	/// </summary>
	public static DependencyProperty LeftHeaderProperty { get; } =
		DependencyProperty.Register(nameof(LeftHeader), typeof(UIElement), typeof(TitleBar), new FrameworkPropertyMetadata(null, OnGenericPropertyChanged));

	/// <summary>
	/// Gets or sets the content to show in the right side of the title bar.
	/// </summary>
	public UIElement RightHeader
	{
		get => (UIElement)GetValue(RightHeaderProperty);
		set => SetValue(RightHeaderProperty, value);
	}

	/// <summary>
	/// Identifies the RightHeader dependency property.
	/// </summary>
	public static DependencyProperty RightHeaderProperty { get; } =
		DependencyProperty.Register(nameof(RightHeader), typeof(UIElement), typeof(TitleBar), new FrameworkPropertyMetadata(null, OnGenericPropertyChanged));

	/// <summary>
	/// Gets or sets the subtitle text to display in the title bar.
	/// </summary>
	public string Subtitle
	{
		get => (string)GetValue(SubtitleProperty);
		set => SetValue(SubtitleProperty, value);
	}

	/// <summary>
	/// Identifies the Subtitle dependency property.
	/// </summary>
	public static DependencyProperty SubtitleProperty { get; } =
		DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(TitleBar), new FrameworkPropertyMetadata(string.Empty, OnGenericPropertyChanged));

	/// <summary>
	/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a TitleBar. Not intended for general use.
	/// </summary>
	public TitleBarTemplateSettings TemplateSettings
	{
		get => (TitleBarTemplateSettings)GetValue(TemplateSettingsProperty);
		private set => SetValue(TemplateSettingsProperty, value);
	}

	/// <summary>
	/// Identifies the TemplateSettings dependency property.
	/// </summary>
	public static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(nameof(TemplateSettings), typeof(TitleBarTemplateSettings), typeof(TitleBar), new FrameworkPropertyMetadata(null, OnGenericPropertyChanged));

	/// <summary>
	/// Gets or sets the title text to display in the title bar.
	/// </summary>
	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	/// <summary>
	/// Identifies the Title dependency property.
	/// </summary>
	public static DependencyProperty TitleProperty { get; } =
		DependencyProperty.Register(nameof(Title), typeof(string), typeof(TitleBar), new FrameworkPropertyMetadata(string.Empty, OnGenericPropertyChanged));

	/// <summary>
	/// Occurs when the back navigation button is invoked.
	/// </summary>
	public event TypedEventHandler<TitleBar, object> BackRequested;

	/// <summary>
	/// Occurs when the pane toggle button is invoked.
	/// </summary>
	public event TypedEventHandler<TitleBar, object> PaneToggleRequested;

	private static void OnGenericPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var titleBar = (TitleBar)sender;
		titleBar.OnPropertyChanged(args);
	}
}
