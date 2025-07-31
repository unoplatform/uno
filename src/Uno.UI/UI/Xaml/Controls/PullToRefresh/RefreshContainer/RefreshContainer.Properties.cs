// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RefreshContainer.properties.cpp, commit de78834

using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public partial class RefreshContainer
{
	/// <summary>
	/// Gets or sets a value that specifies the direction to pull to initiate a refresh.
	/// </summary>
	public RefreshPullDirection PullDirection
	{
		get => (RefreshPullDirection)GetValue(PullDirectionProperty);
		set => SetValue(PullDirectionProperty, value);
	}

	/// <summary>
	/// Identifies the PullDirection dependency property.
	/// </summary>
	public static DependencyProperty PullDirectionProperty { get; } =
		DependencyProperty.Register(nameof(PullDirection), typeof(RefreshPullDirection), typeof(RefreshContainer), new FrameworkPropertyMetadata(RefreshPullDirection.TopToBottom, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the RefreshVisualizer for this container.
	/// </summary>
	public RefreshVisualizer Visualizer
	{
		get => (RefreshVisualizer)GetValue(VisualizerProperty);
		set => SetValue(VisualizerProperty, value);
	}

	/// <summary>
	/// Identifies the Visualizer dependency property.
	/// </summary>
	public static DependencyProperty VisualizerProperty { get; } =
		DependencyProperty.Register(nameof(Visualizer), typeof(RefreshVisualizer), typeof(RefreshContainer), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (RefreshContainer)sender;
		owner.OnPropertyChanged(args);
	}

	/// <summary>
	/// Occurs when an update of the content has been initiated.
	/// </summary>
	public event TypedEventHandler<RefreshContainer, RefreshRequestedEventArgs> RefreshRequested;
}
