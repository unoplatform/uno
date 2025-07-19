// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationViewTemplateSettings.properties.cpp, commit bace2c5

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources
/// when defining templates for a NavigationView. Not intended for general use.
/// </summary>
public partial class NavigationViewTemplateSettings : DependencyObject
{
	/// <summary>
	/// Initializes a new instance of the NavigationViewTemplateSettings class.
	/// </summary>
	public NavigationViewTemplateSettings()
	{
	}

	/// <summary>
	/// Gets the visibility of the back button.
	/// </summary>
	public Visibility BackButtonVisibility
	{
		get => (Visibility)GetValue(BackButtonVisibilityProperty);
		internal set => SetValue(BackButtonVisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the BackButtonVisibility dependency property.
	/// </summary>
	public static DependencyProperty BackButtonVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(BackButtonVisibility), typeof(Visibility), typeof(NavigationViewTemplateSettings), new FrameworkPropertyMetadata(Visibility.Collapsed));

	/// <summary>
	/// Gets the visibility of the left pane.
	/// </summary>
	public Visibility LeftPaneVisibility
	{
		get => (Visibility)GetValue(LeftPaneVisibilityProperty);
		internal set => SetValue(LeftPaneVisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the LeftPaneVisibility dependency property.
	/// </summary>
	public static DependencyProperty LeftPaneVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(LeftPaneVisibility), typeof(Visibility), typeof(NavigationViewTemplateSettings), new FrameworkPropertyMetadata(Visibility.Visible));

	/// <summary>
	/// Gets the width of open pane.
	/// </summary>
	public double OpenPaneLength
	{
		get => (double)GetValue(OpenPaneLengthProperty);
		set => SetValue(OpenPaneLengthProperty, value);
	}

	/// <summary>
	/// Identifies the OpenPaneLength dependency property.
	/// </summary>
	public static DependencyProperty OpenPaneLengthProperty { get; } =
		DependencyProperty.Register(nameof(OpenPaneLength), typeof(double), typeof(NavigationViewTemplateSettings), new FrameworkPropertyMetadata(320.0));

	/// <summary>
	/// Gets the visibility of the overflow button.
	/// </summary>
	public Visibility OverflowButtonVisibility
	{
		get => (Visibility)GetValue(OverflowButtonVisibilityProperty);
		internal set => SetValue(OverflowButtonVisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the OverflowButtonVisibility dependency property.
	/// </summary>
	public static DependencyProperty OverflowButtonVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(OverflowButtonVisibility), typeof(Visibility), typeof(NavigationViewTemplateSettings), new FrameworkPropertyMetadata(Visibility.Collapsed));

	/// <summary>
	/// Gets the visibility of the pane toggle button.
	/// </summary>
	public Visibility PaneToggleButtonVisibility
	{
		get => (Visibility)GetValue(PaneToggleButtonVisibilityProperty);
		internal set => SetValue(PaneToggleButtonVisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the PaneToggleButtonVisibility dependency property.
	/// </summary>
	public static DependencyProperty PaneToggleButtonVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(PaneToggleButtonVisibility), typeof(Visibility), typeof(NavigationViewTemplateSettings), new FrameworkPropertyMetadata(Visibility.Visible));

	/// <summary>
	/// Gets the pane toggle button width.
	/// </summary>
	public double PaneToggleButtonWidth
	{
		get => (double)GetValue(PaneToggleButtonWidthProperty);
		internal set => SetValue(PaneToggleButtonWidthProperty, value);
	}

	/// <summary>
	/// Identifies the PaneToggleButtonWidth dependency property.
	/// </summary>
	public static DependencyProperty PaneToggleButtonWidthProperty { get; } =
		DependencyProperty.Register(nameof(PaneToggleButtonWidth), typeof(double), typeof(NavigationViewTemplateSettings), new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the SelectionFollowsFocus value.
	/// </summary>
	public bool SingleSelectionFollowsFocus
	{
		get => (bool)GetValue(SingleSelectionFollowsFocusProperty);
		internal set => SetValue(SingleSelectionFollowsFocusProperty, value);
	}

	/// <summary>
	/// Identifies the SingleSelectionFollowsFocus dependency property.
	/// </summary>
	public static DependencyProperty SingleSelectionFollowsFocusProperty { get; } =
		DependencyProperty.Register(nameof(SingleSelectionFollowsFocus), typeof(bool), typeof(NavigationViewTemplateSettings), new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets the smaller pane toggle button width.
	/// </summary>
	public double SmallerPaneToggleButtonWidth
	{
		get => (double)GetValue(SmallerPaneToggleButtonWidthProperty);
		internal set => SetValue(SmallerPaneToggleButtonWidthProperty, value);
	}

	/// <summary>
	/// Identifies the SmallerPaneToggleButtonWidth dependency property.
	/// </summary>
	public static DependencyProperty SmallerPaneToggleButtonWidthProperty { get; } =
		DependencyProperty.Register(nameof(SmallerPaneToggleButtonWidth), typeof(double), typeof(NavigationViewTemplateSettings), new FrameworkPropertyMetadata(0.0));


	/// <summary>
	/// Gets the padding value of the top pane.
	/// </summary>
	public double TopPadding
	{
		get => (double)GetValue(TopPaddingProperty);
		internal set => SetValue(TopPaddingProperty, value);
	}

	/// <summary>
	/// Identifies the TopPadding dependency property.
	/// </summary>
	public static DependencyProperty TopPaddingProperty { get; } =
		DependencyProperty.Register(nameof(TopPadding), typeof(double), typeof(NavigationViewTemplateSettings), new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the visibility of the top pane.
	/// </summary>
	public Visibility TopPaneVisibility
	{
		get => (Visibility)GetValue(TopPaneVisibilityProperty);
		internal set => SetValue(TopPaneVisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the TopPaneVisibility dependency property.
	/// </summary>
	public static DependencyProperty TopPaneVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(TopPaneVisibility), typeof(Visibility), typeof(NavigationViewTemplateSettings), new FrameworkPropertyMetadata(Visibility.Collapsed));
}
