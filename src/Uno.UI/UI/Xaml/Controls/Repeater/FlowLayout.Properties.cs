// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlowLayout.idl, commit 4b206bce3

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class FlowLayout
{
	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((FlowLayout)sender).OnPropertyChanged(args);

	/// <summary>
	/// Gets or sets the alignment of items within a line.
	/// </summary>
	/// <value>The alignment value. The default is <see cref="FlowLayoutLineAlignment.Start"/>.</value>
	public FlowLayoutLineAlignment LineAlignment
	{
		get => (FlowLayoutLineAlignment)GetValue(LineAlignmentProperty);
		set => SetValue(LineAlignmentProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="LineAlignment"/> dependency property.
	/// </summary>
	public static DependencyProperty LineAlignmentProperty { get; } = DependencyProperty.Register(
		nameof(LineAlignment),
		typeof(FlowLayoutLineAlignment),
		typeof(FlowLayout),
		new FrameworkPropertyMetadata(FlowLayoutLineAlignment.Start, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a uniform distance (in pixels) between stacked items. It is applied in the direction of the layout's <see cref="Orientation"/>.
	/// </summary>
	/// <value>The uniform distance (in pixels) between stacked items. The default is 0.</value>
	public double LineSpacing
	{
		get => (double)GetValue(LineSpacingProperty);
		set => SetValue(LineSpacingProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="LineSpacing"/> dependency property.
	/// </summary>
	public static DependencyProperty LineSpacingProperty { get; } = DependencyProperty.Register(
		nameof(LineSpacing),
		typeof(double),
		typeof(FlowLayout),
		new FrameworkPropertyMetadata(0.0, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the minimum space between items on the cross-axis.
	/// </summary>
	/// <value>The minimum space (in pixels) between items on the cross-axis. The default is 0.</value>
	public double MinItemSpacing
	{
		get => (double)GetValue(MinItemSpacingProperty);
		set => SetValue(MinItemSpacingProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="MinItemSpacing"/> dependency property.
	/// </summary>
	public static DependencyProperty MinItemSpacingProperty { get; } = DependencyProperty.Register(
		nameof(MinItemSpacing),
		typeof(double),
		typeof(FlowLayout),
		new FrameworkPropertyMetadata(0.0, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the axis along which items are laid out.
	/// </summary>
	/// <value>The axis along which items are laid out. The default is <see cref="Orientation.Horizontal"/>.</value>
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
		typeof(FlowLayout),
		new FrameworkPropertyMetadata(Orientation.Horizontal, OnPropertyChanged));
}
