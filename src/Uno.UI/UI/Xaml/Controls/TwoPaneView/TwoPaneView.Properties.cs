// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TwoPaneView.properties.cpp, commit de78834

using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public partial class TwoPaneView : Microsoft.UI.Xaml.Controls.Control
{
	/// <summary>
	/// Gets or sets the minimum height at which panes are shown in tall mode.
	/// </summary>
	/// <remarks>
	/// The default is 641px.
	/// </remarks>
	public double MinTallModeHeight
	{
		get => (double)GetValue(MinTallModeHeightProperty);
		set => SetValue(MinTallModeHeightProperty, value);
	}

	/// <summary>
	/// Identifies the MinTallModeHeight dependency property.
	/// </summary>
	public static DependencyProperty MinTallModeHeightProperty { get; } =
		DependencyProperty.Register(
			nameof(MinTallModeHeight),
			typeof(double),
			typeof(TwoPaneView),
			new FrameworkPropertyMetadata(c_defaultMinTallModeHeight, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the minimum width at which panes are shown in wide mode.
	/// </summary>
	/// <remarks>
	/// The default is 641px.
	/// </remarks>
	public double MinWideModeWidth
	{
		get => (double)GetValue(MinWideModeWidthProperty);
		set => SetValue(MinWideModeWidthProperty, value);
	}

	/// <summary>
	/// Identifies the MinWideModeWidth dependency property.
	/// </summary>
	public static DependencyProperty MinWideModeWidthProperty { get; } =
		DependencyProperty.Register(
			nameof(MinWideModeWidth),
			typeof(double),
			typeof(TwoPaneView),
			new FrameworkPropertyMetadata(c_defaultMinWideModeWidth, OnPropertyChanged));

	/// <summary>
	/// Gets a value that indicates how panes are shown.
	/// </summary>
	/// <remarks>
	/// Default value is SinglePane.
	/// </remarks>
	public TwoPaneViewMode Mode => (TwoPaneViewMode)GetValue(ModeProperty);

	/// <summary>
	/// Identifies the Mode dependency property.
	/// </summary>
	public static DependencyProperty ModeProperty { get; } =
		DependencyProperty.Register(
			nameof(Mode),
			typeof(TwoPaneViewMode),
			typeof(TwoPaneView),
			new FrameworkPropertyMetadata(TwoPaneViewMode.SinglePane));

	/// <summary>
	/// Gets or sets the content of pane 1.
	/// </summary>
	public UIElement Pane1
	{
		get => (UIElement)GetValue(Pane1Property);
		set => SetValue(Pane1Property, value);
	}

	/// <summary>
	/// Identifies the Pane1 dependency property.
	/// </summary>
	public static DependencyProperty Pane1Property { get; } =
		DependencyProperty.Register(
			nameof(Pane1),
			typeof(UIElement),
			typeof(TwoPaneView),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets the calculated width (in wide mode) or height (in tall mode) of pane 1,
	/// or sets the GridLength value of pane 1.
	/// </summary>
	public GridLength Pane1Length
	{
		get => (GridLength)GetValue(Pane1LengthProperty);
		set => SetValue(Pane1LengthProperty, value);
	}

	/// <summary>
	/// Identifies the Pane1Length dependency property.
	/// </summary>
	public static DependencyProperty Pane1LengthProperty { get; } =
		DependencyProperty.Register(
			nameof(Pane1Length),
			typeof(GridLength),
			typeof(TwoPaneView),
			new FrameworkPropertyMetadata(c_pane1LengthDefault, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the content of pane 2.
	/// </summary>
	public UIElement Pane2
	{
		get => (UIElement)GetValue(Pane2Property);
		set => SetValue(Pane2Property, value);
	}

	/// <summary>
	/// Identifies the Pane2 dependency property.
	/// </summary>
	public static DependencyProperty Pane2Property { get; } =
		DependencyProperty.Register(
			nameof(Pane2),
			typeof(UIElement),
			typeof(TwoPaneView),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets the calculated width (in wide mode) or height (in tall mode) of pane 2,
	/// or sets the GridLength value of pane 2.
	/// </summary>
	public GridLength Pane2Length
	{
		get => (GridLength)GetValue(Pane2LengthProperty);
		set => SetValue(Pane2LengthProperty, value);
	}

	/// <summary>
	/// Identifies the Pane2Length dependency property.
	/// </summary>
	public static DependencyProperty Pane2LengthProperty { get; } =
		DependencyProperty.Register(
			nameof(Pane2Length),
			typeof(GridLength),
			typeof(TwoPaneView),
			new FrameworkPropertyMetadata(c_pane2LengthDefault, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates which pane has priority.
	/// </summary>
	/// <remarks>
	/// The default is Pane1.
	/// </remarks>
	public TwoPaneViewPriority PanePriority
	{
		get => (TwoPaneViewPriority)GetValue(PanePriorityProperty);
		set => SetValue(PanePriorityProperty, value);
	}

	/// <summary>
	/// Identifies the PanePriority dependency property.
	/// </summary>
	public static DependencyProperty PanePriorityProperty { get; } =
		DependencyProperty.Register(
			nameof(PanePriority),
			typeof(TwoPaneViewPriority),
			typeof(TwoPaneView),
			new FrameworkPropertyMetadata(TwoPaneViewPriority.Pane1, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates how panes are shown in tall mode.
	/// </summary>
	/// <remarks>
	/// The default is TopBottom.
	/// </remarks>
	public TwoPaneViewTallModeConfiguration TallModeConfiguration
	{
		get => (TwoPaneViewTallModeConfiguration)GetValue(TallModeConfigurationProperty);
		set => SetValue(TallModeConfigurationProperty, value);
	}

	/// <summary>
	/// Identifies the TallModeConfiguration dependency property.
	/// </summary>
	public static DependencyProperty TallModeConfigurationProperty { get; } =
		DependencyProperty.Register(
			nameof(TallModeConfiguration),
			typeof(TwoPaneViewTallModeConfiguration),
			typeof(TwoPaneView),
			new FrameworkPropertyMetadata(TwoPaneViewTallModeConfiguration.TopBottom, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates how panes are shown in wide mode.
	/// </summary>
	/// <remarks>
	/// The default is LeftRight.
	/// </remarks>
	public TwoPaneViewWideModeConfiguration WideModeConfiguration
	{
		get => (TwoPaneViewWideModeConfiguration)GetValue(WideModeConfigurationProperty);
		set => SetValue(WideModeConfigurationProperty, value);
	}

	/// <summary>
	/// Identifies the WideModeConfiguration dependency property.
	/// </summary>
	public static DependencyProperty WideModeConfigurationProperty { get; } =
		DependencyProperty.Register(
			nameof(WideModeConfiguration),
			typeof(TwoPaneViewWideModeConfiguration),
			typeof(TwoPaneView),
			new FrameworkPropertyMetadata(TwoPaneViewWideModeConfiguration.LeftRight, OnPropertyChanged));

	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (TwoPaneView)sender;
		owner.OnPropertyChanged(args);
	}

	/// <summary>
	/// Occurs when the Mode of the TwoPaneView has changed.
	/// </summary>
	public event TypedEventHandler<TwoPaneView, object> ModeChanged;
}
