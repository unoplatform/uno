// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ProgressBarTemplateSettings.cpp, tag winui3/release/1.7-stable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

public partial class ProgressBarTemplateSettings : DependencyObject
{
	public static DependencyProperty ContainerAnimationStartPositionProperty { get; } = DependencyProperty.Register(
		nameof(ContainerAnimationStartPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

	public double ContainerAnimationStartPosition
	{
		get => (double)GetValue(ContainerAnimationStartPositionProperty);
		set => SetValue(ContainerAnimationStartPositionProperty, value);
	}

	public static DependencyProperty ContainerAnimationEndPositionProperty { get; } = DependencyProperty.Register(
		nameof(ContainerAnimationEndPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

	public double ContainerAnimationEndPosition
	{
		get => (double)GetValue(ContainerAnimationEndPositionProperty);
		set => SetValue(ContainerAnimationEndPositionProperty, value);
	}

	public static DependencyProperty Container2AnimationStartPositionProperty { get; } = DependencyProperty.Register(
		nameof(Container2AnimationStartPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

	public double Container2AnimationStartPosition
	{
		get => (double)GetValue(Container2AnimationStartPositionProperty);
		set => SetValue(Container2AnimationStartPositionProperty, value);
	}

	public static DependencyProperty Container2AnimationEndPositionProperty { get; } = DependencyProperty.Register(
		nameof(Container2AnimationEndPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

	public double Container2AnimationEndPosition
	{
		get => (double)GetValue(Container2AnimationEndPositionProperty);
		set => SetValue(Container2AnimationEndPositionProperty, value);
	}

	public static DependencyProperty EllipseAnimationEndPositionProperty { get; } = DependencyProperty.Register(
		nameof(EllipseAnimationEndPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

	public double EllipseAnimationEndPosition
	{
		get => (double)GetValue(EllipseAnimationEndPositionProperty);
		set => SetValue(EllipseAnimationEndPositionProperty, value);
	}

	public static DependencyProperty EllipseAnimationWellPositionProperty { get; } = DependencyProperty.Register(
		nameof(EllipseAnimationWellPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

	public double EllipseAnimationWellPosition
	{
		get => (double)GetValue(EllipseAnimationWellPositionProperty);
		set => SetValue(EllipseAnimationWellPositionProperty, value);
	}

	public static DependencyProperty EllipseDiameterProperty { get; } = DependencyProperty.Register(
		nameof(EllipseDiameter), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

	public double EllipseDiameter
	{
		get => (double)GetValue(EllipseDiameterProperty);
		set => SetValue(EllipseDiameterProperty, value);
	}

	public static DependencyProperty EllipseOffsetProperty { get; } = DependencyProperty.Register(
		nameof(EllipseOffset), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

	public double EllipseOffset
	{
		get => (double)GetValue(EllipseOffsetProperty);
		set => SetValue(EllipseOffsetProperty, value);
	}

	public static DependencyProperty ContainerAnimationMidPositionProperty { get; } = DependencyProperty.Register(
		nameof(ContainerAnimationMidPosition), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

	public double ContainerAnimationMidPosition
	{
		get => (double)GetValue(ContainerAnimationMidPositionProperty);
		set => SetValue(ContainerAnimationMidPositionProperty, value);
	}

	public static DependencyProperty IndicatorLengthDeltaProperty { get; } = DependencyProperty.Register(
		nameof(IndicatorLengthDelta), typeof(double), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(double)));

	public double IndicatorLengthDelta
	{
		get => (double)GetValue(IndicatorLengthDeltaProperty);
		set => SetValue(IndicatorLengthDeltaProperty, value);
	}

	public static DependencyProperty ClipRectProperty { get; } = DependencyProperty.Register(
		nameof(ClipRect), typeof(RectangleGeometry), typeof(ProgressBarTemplateSettings), new FrameworkPropertyMetadata(default(RectangleGeometry)));

	public RectangleGeometry ClipRect
	{
		get => (RectangleGeometry)GetValue(ClipRectProperty);
		set => SetValue(ClipRectProperty, value);
	}
}
