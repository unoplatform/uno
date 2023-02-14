// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RefreshVisualizer.properties.cpp, commit de78834

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public partial class RefreshVisualizer
{
	/// <summary>
	/// Gets or sets the content of the visualizer.
	/// </summary>
	public UIElement? Content
	{
		get => (UIElement)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	/// <summary>
	/// Identifies the Content dependency property.
	/// </summary>
	public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(
			nameof(Content),
			typeof(UIElement),
			typeof(RefreshVisualizer),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Identifies the InfoProvider dependency property.
	/// </summary>
	public static DependencyProperty InfoProviderProperty { get; } =
		DependencyProperty.Register(
			nameof(InfoProvider),
			typeof(object),
			typeof(RefreshVisualizer),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates the orientation of the visualizer.
	/// </summary>
	public RefreshVisualizerOrientation Orientation
	{
		get => (RefreshVisualizerOrientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>
	/// Identifies the Orientation dependency property.
	/// </summary>
	public static DependencyProperty OrientationProperty { get; } =
		DependencyProperty.Register(
			nameof(Orientation),
			typeof(RefreshVisualizerOrientation),
			typeof(RefreshVisualizer),
			new FrameworkPropertyMetadata(RefreshVisualizerOrientation.Auto, OnPropertyChanged));

	/// <summary>
	/// Gets a value that indicates the state of the visualizer.
	/// </summary>
	public RefreshVisualizerState State
	{
		get => (RefreshVisualizerState)GetValue(StateProperty);
		private set => SetValue(StateProperty, value);
	}

	/// <summary>
	/// Gets a value that indicates the state of the visualizer.
	/// </summary>
	public static DependencyProperty StateProperty { get; } =
		DependencyProperty.Register(
			nameof(State),
			typeof(RefreshVisualizerState),
			typeof(RefreshVisualizer),
			new FrameworkPropertyMetadata(RefreshVisualizerState.Idle, OnPropertyChanged));

	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (RefreshVisualizer)sender;
		owner.OnPropertyChanged(args);
	}

	/// <summary>
	/// Occurs when an update of the content has been initiated.
	/// </summary>
	public event TypedEventHandler<RefreshVisualizer, RefreshRequestedEventArgs>? RefreshRequested;

	/// <summary>
	/// Occurs when an update of the content has been initiated.
	/// </summary>
	public event TypedEventHandler<RefreshVisualizer, RefreshStateChangedEventArgs>? RefreshStateChanged;
}
