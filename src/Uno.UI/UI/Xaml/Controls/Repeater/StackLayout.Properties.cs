// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference StackLayout.idl, commit 4b206bce3

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class StackLayout
{
	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((StackLayout)sender).OnPropertyChanged(args);

	/// <summary>
	/// Gets or sets the axis along which items are laid out.
	/// </summary>
	/// <value>The axis along which items are laid out. The default is <see cref="Orientation.Vertical"/>.</value>
	public Orientation Orientation
	{
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="Orientation"/> dependency property.
	/// </summary>
	public static DependencyProperty OrientationProperty { get; } = DependencyProperty.Register(
		nameof(Orientation),
		typeof(Orientation),
		typeof(StackLayout),
		new FrameworkPropertyMetadata(Orientation.Vertical, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a uniform distance (in pixels) between stacked items.
	/// It is applied in the direction of the layout's <see cref="Orientation"/>.
	/// </summary>
	/// <value>The uniform distance (in pixels) between stacked items. The default is 0.</value>
	public double Spacing
	{
		get => (double)GetValue(SpacingProperty);
		set => SetValue(SpacingProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="Spacing"/> dependency property.
	/// </summary>
	public static DependencyProperty SpacingProperty { get; } = DependencyProperty.Register(
		nameof(Spacing),
		typeof(double),
		typeof(StackLayout),
		new FrameworkPropertyMetadata(0.0, OnPropertyChanged));

	// [MUX_PREVIEW] in StackLayout.idl — default true.
	/// <summary>
	/// Gets or sets a value that indicates whether the layout virtualizes items.
	/// </summary>
	/// <value><c>true</c> if the layout virtualizes items; otherwise, <c>false</c>. The default is <c>true</c>.</value>
	public bool IsVirtualizationEnabled
	{
		get => (bool)GetValue(IsVirtualizationEnabledProperty);
		set => SetValue(IsVirtualizationEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="IsVirtualizationEnabled"/> dependency property.
	/// </summary>
	public static DependencyProperty IsVirtualizationEnabledProperty { get; } = DependencyProperty.Register(
		nameof(IsVirtualizationEnabled),
		typeof(bool),
		typeof(StackLayout),
		new FrameworkPropertyMetadata(true, OnPropertyChanged));
}
