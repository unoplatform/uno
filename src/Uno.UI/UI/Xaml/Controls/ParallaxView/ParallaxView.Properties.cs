// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ParallaxView.idl, ParallaxView.properties.* (MUX_PROPERTY_CHANGED_CALLBACK), commit 5f9e85113

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class ParallaxView
{
	/// <summary>
	/// Gets or sets the background content of the ParallaxView.
	/// </summary>
	public UIElement Child
	{
		get => (UIElement)GetValue(ChildProperty);
		set => SetValue(ChildProperty, value);
	}

	/// <summary>
	/// Identifies the Child dependency property.
	/// </summary>
	public static DependencyProperty ChildProperty { get; } =
		DependencyProperty.Register(
			nameof(Child),
			typeof(UIElement),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Represents the horizontal range of motion of the child element.
	/// </summary>
	public double HorizontalShift
	{
		get => (double)GetValue(HorizontalShiftProperty);
		set => SetValue(HorizontalShiftProperty, value);
	}

	/// <summary>
	/// Identifies the HorizontalShift dependency property.
	/// </summary>
	public static DependencyProperty HorizontalShiftProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalShift),
			typeof(double),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(0.0, OnPropertyChanged));

	/// <summary>
	/// Represents the horizontal scroll offset at which the parallax motion ends.
	/// </summary>
	public double HorizontalSourceEndOffset
	{
		get => (double)GetValue(HorizontalSourceEndOffsetProperty);
		set => SetValue(HorizontalSourceEndOffsetProperty, value);
	}

	/// <summary>
	/// Identifies the HorizontalSourceEndOffset dependency property.
	/// </summary>
	public static DependencyProperty HorizontalSourceEndOffsetProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalSourceEndOffset),
			typeof(double),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(0.0, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that determines how the horizontal source offset values of a ParallaxView are interpreted.
	/// </summary>
	public ParallaxSourceOffsetKind HorizontalSourceOffsetKind
	{
		get => (ParallaxSourceOffsetKind)GetValue(HorizontalSourceOffsetKindProperty);
		set => SetValue(HorizontalSourceOffsetKindProperty, value);
	}

	/// <summary>
	/// Identifies the HorizontalSourceOffsetKind dependency property.
	/// </summary>
	public static DependencyProperty HorizontalSourceOffsetKindProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalSourceOffsetKind),
			typeof(ParallaxSourceOffsetKind),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(ParallaxSourceOffsetKind.Relative, OnPropertyChanged));

	/// <summary>
	/// Represents the horizontal scroll offset at which parallax motion starts.
	/// </summary>
	public double HorizontalSourceStartOffset
	{
		get => (double)GetValue(HorizontalSourceStartOffsetProperty);
		set => SetValue(HorizontalSourceStartOffsetProperty, value);
	}

	/// <summary>
	/// Identifies the HorizontalSourceStartOffset dependency property.
	/// </summary>
	public static DependencyProperty HorizontalSourceStartOffsetProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalSourceStartOffset),
			typeof(double),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(0.0, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the horizontal parallax ratio is clamped to a specified percentage of the source scroll velocity.
	/// </summary>
	public bool IsHorizontalShiftClamped
	{
		get => (bool)GetValue(IsHorizontalShiftClampedProperty);
		set => SetValue(IsHorizontalShiftClampedProperty, value);
	}

	/// <summary>
	/// Identifies the IsHorizontalShiftClamped dependency property.
	/// </summary>
	public static DependencyProperty IsHorizontalShiftClampedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsHorizontalShiftClamped),
			typeof(bool),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(true, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the vertical parallax ratio is clamped to a specified percentage of the source scroll velocity.
	/// </summary>
	public bool IsVerticalShiftClamped
	{
		get => (bool)GetValue(IsVerticalShiftClampedProperty);
		set => SetValue(IsVerticalShiftClampedProperty, value);
	}

	/// <summary>
	/// Identifies the IsVerticalShiftClamped dependency property.
	/// </summary>
	public static DependencyProperty IsVerticalShiftClampedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsVerticalShiftClamped),
			typeof(bool),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(true, OnPropertyChanged));

	/// <summary>
	/// Clamps the horizontal parallax ratio to the specified percentage of the source scroll velocity.
	/// </summary>
	public double MaxHorizontalShiftRatio
	{
		get => (double)GetValue(MaxHorizontalShiftRatioProperty);
		set => SetValue(MaxHorizontalShiftRatioProperty, value);
	}

	/// <summary>
	/// Identifies the MaxHorizontalShiftRatio dependency property.
	/// </summary>
	public static DependencyProperty MaxHorizontalShiftRatioProperty { get; } =
		DependencyProperty.Register(
			nameof(MaxHorizontalShiftRatio),
			typeof(double),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(1.0, OnPropertyChanged));

	/// <summary>
	/// Clamps the vertical parallax ratio to the specified percentage of the source scroll velocity.
	/// </summary>
	public double MaxVerticalShiftRatio
	{
		get => (double)GetValue(MaxVerticalShiftRatioProperty);
		set => SetValue(MaxVerticalShiftRatioProperty, value);
	}

	/// <summary>
	/// Identifies the MaxVerticalShiftRatio dependency property.
	/// </summary>
	public static DependencyProperty MaxVerticalShiftRatioProperty { get; } =
		DependencyProperty.Register(
			nameof(MaxVerticalShiftRatio),
			typeof(double),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(1.0, OnPropertyChanged));

	/// <summary>
	/// The element that either is or contains the ScrollViewer that controls the parallax operation.
	/// </summary>
	public UIElement Source
	{
		get => (UIElement)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	/// <summary>
	/// Identifies the Source dependency property.
	/// </summary>
	public static DependencyProperty SourceProperty { get; } =
		DependencyProperty.Register(
			nameof(Source),
			typeof(UIElement),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>
	/// Represents the vertical range of motion of the child element.
	/// </summary>
	public double VerticalShift
	{
		get => (double)GetValue(VerticalShiftProperty);
		set => SetValue(VerticalShiftProperty, value);
	}

	/// <summary>
	/// Identifies the VerticalShift dependency property.
	/// </summary>
	public static DependencyProperty VerticalShiftProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalShift),
			typeof(double),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(0.0, OnPropertyChanged));

	/// <summary>
	/// Represents the vertical scroll offset at which the parallax motion ends.
	/// </summary>
	public double VerticalSourceEndOffset
	{
		get => (double)GetValue(VerticalSourceEndOffsetProperty);
		set => SetValue(VerticalSourceEndOffsetProperty, value);
	}

	/// <summary>
	/// Identifies the VerticalSourceEndOffset dependency property.
	/// </summary>
	public static DependencyProperty VerticalSourceEndOffsetProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalSourceEndOffset),
			typeof(double),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(0.0, OnPropertyChanged));

	/// <summary>
	/// Gets or sets a value that determines how the vertical source offset values of a ParallaxView are interpreted.
	/// </summary>
	public ParallaxSourceOffsetKind VerticalSourceOffsetKind
	{
		get => (ParallaxSourceOffsetKind)GetValue(VerticalSourceOffsetKindProperty);
		set => SetValue(VerticalSourceOffsetKindProperty, value);
	}

	/// <summary>
	/// Identifies the VerticalSourceOffsetKind dependency property.
	/// </summary>
	public static DependencyProperty VerticalSourceOffsetKindProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalSourceOffsetKind),
			typeof(ParallaxSourceOffsetKind),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(ParallaxSourceOffsetKind.Relative, OnPropertyChanged));

	/// <summary>
	/// Represents the vertical scroll offset at which parallax motion starts.
	/// </summary>
	public double VerticalSourceStartOffset
	{
		get => (double)GetValue(VerticalSourceStartOffsetProperty);
		set => SetValue(VerticalSourceStartOffsetProperty, value);
	}

	/// <summary>
	/// Identifies the VerticalSourceStartOffset dependency property.
	/// </summary>
	public static DependencyProperty VerticalSourceStartOffsetProperty { get; } =
		DependencyProperty.Register(
			nameof(VerticalSourceStartOffset),
			typeof(double),
			typeof(ParallaxView),
			new FrameworkPropertyMetadata(0.0, OnPropertyChanged));

	private static void OnPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		(sender as ParallaxView)?.OnPropertyChanged(args);
	}
}
