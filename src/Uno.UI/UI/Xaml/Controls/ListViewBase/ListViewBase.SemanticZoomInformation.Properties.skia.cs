// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Microsoft.UI.Xaml.Controls.cs, tag winui3/release/1.8.4, commit dc46907e92

#nullable enable

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBase
{
	/// <summary>
	/// Gets or sets a value indicating whether this view is the currently active view in the SemanticZoom.
	/// </summary>
	public bool IsActiveView
	{
		get => (bool)GetValue(IsActiveViewProperty);
		set => SetValue(IsActiveViewProperty, value);
	}

	/// <summary>
	/// Identifies the IsActiveView dependency property.
	/// </summary>
	public static DependencyProperty IsActiveViewProperty { get; } =
		DependencyProperty.Register(
			nameof(IsActiveView),
			typeof(bool),
			typeof(ListViewBase),
			new FrameworkPropertyMetadata(default(bool)));

	/// <summary>
	/// Gets or sets a value indicating whether this view is the ZoomedInView of the SemanticZoom.
	/// </summary>
	public bool IsZoomedInView
	{
		get => (bool)GetValue(IsZoomedInViewProperty);
		set => SetValue(IsZoomedInViewProperty, value);
	}

	/// <summary>
	/// Identifies the IsZoomedInView dependency property.
	/// </summary>
	public static DependencyProperty IsZoomedInViewProperty { get; } =
		DependencyProperty.Register(
			nameof(IsZoomedInView),
			typeof(bool),
			typeof(ListViewBase),
			new FrameworkPropertyMetadata(defaultValue: true));

	/// <summary>
	/// Gets or sets the SemanticZoom that controls navigation behavior.
	/// </summary>
	public SemanticZoom? SemanticZoomOwner
	{
		get => (SemanticZoom?)GetValue(SemanticZoomOwnerProperty);
		set => SetValue(SemanticZoomOwnerProperty, value);
	}

	/// <summary>
	/// Identifies the SemanticZoomOwner dependency property.
	/// </summary>
	public static DependencyProperty SemanticZoomOwnerProperty { get; } =
		DependencyProperty.Register(
			nameof(SemanticZoomOwner),
			typeof(SemanticZoom),
			typeof(ListViewBase),
			new FrameworkPropertyMetadata(default(SemanticZoom)));
}
