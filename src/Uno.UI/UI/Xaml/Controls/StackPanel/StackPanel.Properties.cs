﻿using System;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

partial class StackPanel
{
	/// <summary>
	/// Gets a value that indicates whether the horizontal snap points
	/// for the StackPanel are equidistant from each other.
	/// </summary>
	public bool AreHorizontalSnapPointsRegular => AreHorizontalSnapPointsRegularImpl();

	/// <summary>
	/// Gets or sets a value that indicates whether the generated snap points
	/// used for panning in the StackPanel are equidistant from each other.
	/// </summary>
	public bool AreScrollSnapPointsRegular
	{
		get => GetAreScrollSnapPointsRegularValue();
		set => SetAreScrollSnapPointsRegularValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = false)]
	public static DependencyProperty AreScrollSnapPointsRegularProperty { get; } = CreateAreScrollSnapPointsRegularProperty();

	/// <summary>
	/// Gets a value that indicates whether the vertical snap points
	/// for the StackPanel are equidistant from each other.
	/// </summary>
	public bool AreVerticalSnapPointsRegular => AreVerticalSnapPointsRegularImpl();

	/// <summary>
	/// Gets or sets a value that indicates how far the background
	/// extends in relation to this element's border.
	/// </summary>
	public BackgroundSizing BackgroundSizing
	{
		get => GetBackgroundSizingValue();
		set => SetBackgroundSizingValue(value);
	}

	/// <summary>
	/// Identifies the BackgroundSizing dependency property.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = default(BackgroundSizing))]
	public static DependencyProperty BackgroundSizingProperty { get; } = CreateBackgroundSizingProperty();

	/// <summary>
	/// Gets or sets a brush that describes the border fill of the panel.
	/// </summary>
	public Brush BorderBrush
	{
		get => GetBorderBrushValue();
		set => SetBorderBrushValue(value);
	}

	/// <summary>
	/// Identifies the BorderBrush dependency property.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = default(Brush))]
	public static DependencyProperty BorderBrushProperty { get; } = CreateBorderBrushProperty();

	/// <summary>
	/// Gets or sets the border thickness of the panel.
	/// </summary>
	public Thickness BorderThickness
	{
		get => (Thickness)GetValue(BorderThicknessProperty);
		set => SetValue(BorderThicknessProperty, value);
	}

	/// <summary>
	/// Identifies the BorderThickness dependency property.
	/// </summary>
	[GeneratedDependencyProperty]
	public static DependencyProperty BorderThicknessProperty { get; } = CreateBorderThicknessProperty();

	private static Thickness GetBorderThicknessDefaultValue() => default;

	/// <summary>
	/// Gets or sets the radius for the corners of the panel's border.
	/// </summary>
	public CornerRadius CornerRadius
	{
		get => (CornerRadius)GetValue(CornerRadiusProperty);
		set => SetValue(CornerRadiusProperty, value);
	}

	/// <summary>
	/// Identifies the CornerRadius dependency property.
	/// </summary>
	[GeneratedDependencyProperty]
	public static DependencyProperty CornerRadiusProperty { get; } = CreateCornerRadiusProperty();

	private static CornerRadius GetCornerRadiusDefaultValue() => default;

	/// <summary>
	/// Gets or sets the dimension by which child elements are stacked.
	/// </summary>
	public Orientation Orientation
	{
		get => GetOrientationValue();
		set => SetOrientationValue(value);
	}

	/// <summary>
	/// Identifies the Orientation  dependency property.
	/// </summary>
	[GeneratedDependencyProperty(Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
	public static DependencyProperty OrientationProperty { get; } = CreateOrientationProperty();

	/// <summary>
	/// Gets or sets the distance between the border and its child object.
	/// </summary>
	public Thickness Padding
	{
		get => GetPaddingValue();
		set => SetPaddingValue(value);
	}

	/// <summary>
	/// Identifies the Padding dependency property.
	/// </summary>
	[GeneratedDependencyProperty]
	public static DependencyProperty PaddingProperty { get; } = CreatePaddingProperty();

	private static Thickness GetPaddingDefaultValue() => default;

	/// <summary>
	/// Gets or sets a uniform distance (in pixels) between stacked items.
	/// It is applied in the direction of the StackPanel's Orientation.
	/// </summary>
	public double Spacing
	{
		get => GetSpacingValue();
		set => SetSpacingValue(value);
	}

	/// <summary>
	/// Identifies the Spacing dependency property.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = 0.0, Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
	public static DependencyProperty SpacingProperty { get; } = CreateSpacingProperty();

	private EventHandler<object> _horizontalSnapPointsChanged;

	/// <summary>
	/// Occurs when the measurements for horizontal snap points change.
	/// </summary>
	public event EventHandler<object> HorizontalSnapPointsChanged
	{
		add => AddHorizontalSnapPointsChanged(value);
		remove => RemoveHorizontalSnapPointsChanged(value);
	}

	private EventHandler<object> _verticalSnapPointsChanged;

	/// <summary>
	/// Occurs when the measurements for vertical snap points change.
	/// </summary>
	public event EventHandler<object> VerticalSnapPointsChanged
	{
		add => AddVerticalSnapPointsChanged(value);
		remove => RemoveVerticalSnapPointsChanged(value);
	}
}
