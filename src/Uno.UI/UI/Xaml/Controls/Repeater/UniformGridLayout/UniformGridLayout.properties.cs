// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference UniformGridLayout.idl, commit 4b206bce3

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class UniformGridLayout
{
	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((UniformGridLayout)sender).OnPropertyChanged(args);

	/// <summary>
	/// Gets or sets a value that indicates how items are aligned on the non-scrolling or non-virtualizing dimension.
	/// </summary>
	/// <value>An enumeration value that indicates how items are aligned. The default is <see cref="UniformGridLayoutItemsJustification.Start"/>.</value>
	public UniformGridLayoutItemsJustification ItemsJustification
	{
		get => (UniformGridLayoutItemsJustification)GetValue(ItemsJustificationProperty);
		set => SetValue(ItemsJustificationProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="ItemsJustification"/> dependency property.
	/// </summary>
	public static DependencyProperty ItemsJustificationProperty { get; } = DependencyProperty.Register(
		nameof(ItemsJustification),
		typeof(UniformGridLayoutItemsJustification),
		typeof(UniformGridLayout),
		new FrameworkPropertyMetadata(UniformGridLayoutItemsJustification.Start, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates how items are sized to fill the available space.
	/// </summary>
	/// <value>An enumeration value that indicates how items are sized to fill the available space. The default is <see cref="UniformGridLayoutItemsStretch.None"/>.</value>
	public UniformGridLayoutItemsStretch ItemsStretch
	{
		get => (UniformGridLayoutItemsStretch)GetValue(ItemsStretchProperty);
		set => SetValue(ItemsStretchProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="ItemsStretch"/> dependency property.
	/// </summary>
	public static DependencyProperty ItemsStretchProperty { get; } = DependencyProperty.Register(
		nameof(ItemsStretch),
		typeof(UniformGridLayoutItemsStretch),
		typeof(UniformGridLayout),
		new FrameworkPropertyMetadata(UniformGridLayoutItemsStretch.None, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the maximum row or column count.
	/// </summary>
	/// <value>The maximum row or column count, or -1 for an unlimited number of rows or columns. The default is -1.</value>
	public int MaximumRowsOrColumns
	{
		get => (int)GetValue(MaximumRowsOrColumnsProperty);
		set => SetValue(MaximumRowsOrColumnsProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="MaximumRowsOrColumns"/> dependency property.
	/// </summary>
	public static DependencyProperty MaximumRowsOrColumnsProperty { get; } = DependencyProperty.Register(
		nameof(MaximumRowsOrColumns),
		typeof(int),
		typeof(UniformGridLayout),
		new FrameworkPropertyMetadata(-1, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the minimum spacing between items on the non-scrolling or non-virtualizing dimension.
	/// </summary>
	/// <value>The minimum spacing between items on the non-scrolling or non-virtualizing dimension. The default is 0.</value>
	public double MinColumnSpacing
	{
		get => (double)GetValue(MinColumnSpacingProperty);
		set => SetValue(MinColumnSpacingProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="MinColumnSpacing"/> dependency property.
	/// </summary>
	public static DependencyProperty MinColumnSpacingProperty { get; } = DependencyProperty.Register(
		nameof(MinColumnSpacing),
		typeof(double),
		typeof(UniformGridLayout),
		new FrameworkPropertyMetadata(0.0, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the minimum height of an item.
	/// </summary>
	/// <value>The minimum height (in pixels) of an item. The default is 0.</value>
	public double MinItemHeight
	{
		get => (double)GetValue(MinItemHeightProperty);
		set => SetValue(MinItemHeightProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="MinItemHeight"/> dependency property.
	/// </summary>
	public static DependencyProperty MinItemHeightProperty { get; } = DependencyProperty.Register(
		nameof(MinItemHeight),
		typeof(double),
		typeof(UniformGridLayout),
		new FrameworkPropertyMetadata(0.0, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the minimum width of an item.
	/// </summary>
	/// <value>The minimum width (in pixels) of an item. The default is 0.</value>
	public double MinItemWidth
	{
		get => (double)GetValue(MinItemWidthProperty);
		set => SetValue(MinItemWidthProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="MinItemWidth"/> dependency property.
	/// </summary>
	public static DependencyProperty MinItemWidthProperty { get; } = DependencyProperty.Register(
		nameof(MinItemWidth),
		typeof(double),
		typeof(UniformGridLayout),
		new FrameworkPropertyMetadata(0.0, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the minimum spacing between items on the scrolling or virtualizing dimension.
	/// </summary>
	/// <value>The minimum spacing between items on the scrolling or virtualizing dimension. The default is 0.</value>
	public double MinRowSpacing
	{
		get => (double)GetValue(MinRowSpacingProperty);
		set => SetValue(MinRowSpacingProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="MinRowSpacing"/> dependency property.
	/// </summary>
	public static DependencyProperty MinRowSpacingProperty { get; } = DependencyProperty.Register(
		nameof(MinRowSpacing),
		typeof(double),
		typeof(UniformGridLayout),
		new FrameworkPropertyMetadata(0.0, OnPropertyChanged));

	/// <summary>
	/// Gets or sets the axis along which items are laid out.
	/// </summary>
	/// <value>An enumeration value that indicates the axis along which items are laid out. The default is <see cref="Orientation.Horizontal"/>.</value>
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
		typeof(UniformGridLayout),
		new FrameworkPropertyMetadata(Orientation.Horizontal, OnPropertyChanged));
}
