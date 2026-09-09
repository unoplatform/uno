// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ProgressBarTemplateSettings.cpp, tag winui3/release/1.7-stable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers;

namespace Microsoft.UI.Xaml.Controls;

public partial class ProgressBarTemplateSettings : DependencyObject
{
	public static DependencyProperty ContainerAnimationStartPositionProperty { get; } = DependencyProperty.Register(
		nameof(ContainerAnimationStartPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(Boxes.DoubleBoxes.Zero));

	public double ContainerAnimationStartPosition
	{
		get => (double)GetValue(ContainerAnimationStartPositionProperty);
		set => SetValue(ContainerAnimationStartPositionProperty, Boxes.Box(value));
	}

	public static DependencyProperty ContainerAnimationEndPositionProperty { get; } = DependencyProperty.Register(
		nameof(ContainerAnimationEndPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(Boxes.DoubleBoxes.Zero));

	public double ContainerAnimationEndPosition
	{
		get => (double)GetValue(ContainerAnimationEndPositionProperty);
		set => SetValue(ContainerAnimationEndPositionProperty, Boxes.Box(value));
	}

	public static DependencyProperty Container2AnimationStartPositionProperty { get; } = DependencyProperty.Register(
		nameof(Container2AnimationStartPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(Boxes.DoubleBoxes.Zero));

	public double Container2AnimationStartPosition
	{
		get => (double)GetValue(Container2AnimationStartPositionProperty);
		set => SetValue(Container2AnimationStartPositionProperty, Boxes.Box(value));
	}

	public static DependencyProperty Container2AnimationEndPositionProperty { get; } = DependencyProperty.Register(
		nameof(Container2AnimationEndPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(Boxes.DoubleBoxes.Zero));

	public double Container2AnimationEndPosition
	{
		get => (double)GetValue(Container2AnimationEndPositionProperty);
		set => SetValue(Container2AnimationEndPositionProperty, Boxes.Box(value));
	}

	public static DependencyProperty EllipseAnimationEndPositionProperty { get; } = DependencyProperty.Register(
		nameof(EllipseAnimationEndPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(Boxes.DoubleBoxes.Zero));

	public double EllipseAnimationEndPosition
	{
		get => (double)GetValue(EllipseAnimationEndPositionProperty);
		set => SetValue(EllipseAnimationEndPositionProperty, Boxes.Box(value));
	}

	public static DependencyProperty EllipseAnimationWellPositionProperty { get; } = DependencyProperty.Register(
		nameof(EllipseAnimationWellPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(Boxes.DoubleBoxes.Zero));

	public double EllipseAnimationWellPosition
	{
		get => (double)GetValue(EllipseAnimationWellPositionProperty);
		set => SetValue(EllipseAnimationWellPositionProperty, Boxes.Box(value));
	}

	public static DependencyProperty EllipseDiameterProperty { get; } = DependencyProperty.Register(
		nameof(EllipseDiameter), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(Boxes.DoubleBoxes.Zero));

	public double EllipseDiameter
	{
		get => (double)GetValue(EllipseDiameterProperty);
		set => SetValue(EllipseDiameterProperty, Boxes.Box(value));
	}

	public static DependencyProperty EllipseOffsetProperty { get; } = DependencyProperty.Register(
		nameof(EllipseOffset), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(Boxes.DoubleBoxes.Zero));

	public double EllipseOffset
	{
		get => (double)GetValue(EllipseOffsetProperty);
		set => SetValue(EllipseOffsetProperty, Boxes.Box(value));
	}

	public static DependencyProperty ContainerAnimationMidPositionProperty { get; } = DependencyProperty.Register(
		nameof(ContainerAnimationMidPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(Boxes.DoubleBoxes.Zero));

	public double ContainerAnimationMidPosition
	{
		get => (double)GetValue(ContainerAnimationMidPositionProperty);
		set => SetValue(ContainerAnimationMidPositionProperty, Boxes.Box(value));
	}

	public static DependencyProperty IndicatorLengthDeltaProperty { get; } = DependencyProperty.Register(
		nameof(IndicatorLengthDelta), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(Boxes.DoubleBoxes.Zero));

	public double IndicatorLengthDelta
	{
		get => (double)GetValue(IndicatorLengthDeltaProperty);
		set => SetValue(IndicatorLengthDeltaProperty, Boxes.Box(value));
	}

	public static DependencyProperty ClipRectProperty { get; } = DependencyProperty.Register(
		nameof(ClipRect), typeof(RectangleGeometry), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(RectangleGeometry)));

	public RectangleGeometry ClipRect
	{
		get => (RectangleGeometry)GetValue(ClipRectProperty);
		set => SetValue(ClipRectProperty, value);
	}
}
