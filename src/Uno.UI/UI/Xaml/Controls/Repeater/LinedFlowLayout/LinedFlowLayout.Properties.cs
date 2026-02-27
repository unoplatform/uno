// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayout.properties.cpp, tag winui3/release/1.8.4

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class LinedFlowLayout
{
	// Default values
	private const LinedFlowLayoutItemsJustification DefaultItemsJustification = LinedFlowLayoutItemsJustification.Start;
	private const LinedFlowLayoutItemsStretch DefaultItemsStretch = LinedFlowLayoutItemsStretch.None;
	private const double DefaultActualLineHeight = 0.0;
	private const double DefaultLineSpacing = 0.0;
	private const double DefaultMinItemSpacing = 0.0;
	private static readonly double DefaultLineHeight = double.NaN;

	public static DependencyProperty ActualLineHeightProperty { get; } = DependencyProperty.Register(
		nameof(ActualLineHeight),
		typeof(double),
		typeof(LinedFlowLayout),
		new FrameworkPropertyMetadata(DefaultActualLineHeight));

	public static DependencyProperty ItemsJustificationProperty { get; } = DependencyProperty.Register(
		nameof(ItemsJustification),
		typeof(LinedFlowLayoutItemsJustification),
		typeof(LinedFlowLayout),
		new FrameworkPropertyMetadata(DefaultItemsJustification, OnPropertyChanged));

	public static DependencyProperty ItemsStretchProperty { get; } = DependencyProperty.Register(
		nameof(ItemsStretch),
		typeof(LinedFlowLayoutItemsStretch),
		typeof(LinedFlowLayout),
		new FrameworkPropertyMetadata(DefaultItemsStretch, OnPropertyChanged));

	public static DependencyProperty LineHeightProperty { get; } = DependencyProperty.Register(
		nameof(LineHeight),
		typeof(double),
		typeof(LinedFlowLayout),
		new FrameworkPropertyMetadata(DefaultLineHeight, OnPropertyChanged));

	public static DependencyProperty LineSpacingProperty { get; } = DependencyProperty.Register(
		nameof(LineSpacing),
		typeof(double),
		typeof(LinedFlowLayout),
		new FrameworkPropertyMetadata(DefaultLineSpacing, OnPropertyChanged));

	public static DependencyProperty MinItemSpacingProperty { get; } = DependencyProperty.Register(
		nameof(MinItemSpacing),
		typeof(double),
		typeof(LinedFlowLayout),
		new FrameworkPropertyMetadata(DefaultMinItemSpacing, OnPropertyChanged));

	public double ActualLineHeight
	{
		get => (double)GetValue(ActualLineHeightProperty);
		private set => SetValue(ActualLineHeightProperty, value);
	}

	public LinedFlowLayoutItemsJustification ItemsJustification
	{
		get => (LinedFlowLayoutItemsJustification)GetValue(ItemsJustificationProperty);
		set => SetValue(ItemsJustificationProperty, value);
	}

	public LinedFlowLayoutItemsStretch ItemsStretch
	{
		get => (LinedFlowLayoutItemsStretch)GetValue(ItemsStretchProperty);
		set => SetValue(ItemsStretchProperty, value);
	}

	public double LineHeight
	{
		get => (double)GetValue(LineHeightProperty);
		set => SetValue(LineHeightProperty, value);
	}

	public double LineSpacing
	{
		get => (double)GetValue(LineSpacingProperty);
		set => SetValue(LineSpacingProperty, value);
	}

	public double MinItemSpacing
	{
		get => (double)GetValue(MinItemSpacingProperty);
		set => SetValue(MinItemSpacingProperty, value);
	}

	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		((LinedFlowLayout)sender).OnPropertyChanged(args);
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.Property != ActualLineHeightProperty)
		{
			InvalidateLayout();
		}
	}

	// Events
	private TypedEventHandler<LinedFlowLayout, LinedFlowLayoutItemsInfoRequestedEventArgs> _itemsInfoRequested;
	private TypedEventHandler<LinedFlowLayout, object> _itemsUnlocked;

	public event TypedEventHandler<LinedFlowLayout, LinedFlowLayoutItemsInfoRequestedEventArgs> ItemsInfoRequested
	{
		add => _itemsInfoRequested += value;
		remove => _itemsInfoRequested -= value;
	}

	public event TypedEventHandler<LinedFlowLayout, object> ItemsUnlocked
	{
		add => _itemsUnlocked += value;
		remove => _itemsUnlocked -= value;
	}
}
