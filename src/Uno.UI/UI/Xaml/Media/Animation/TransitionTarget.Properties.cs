// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference XamlOM Microsoft.UI.Xaml.Media.Animation.cs (TransitionTarget), tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using Uno.UI.Helpers.Boxes;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class TransitionTarget
{
	internal double Opacity
	{
		get => (double)GetValue(OpacityProperty);
		set => SetValue(OpacityProperty, value);
	}

	internal static DependencyProperty OpacityProperty { get; } =
		DependencyProperty.Register(
			nameof(Opacity),
			typeof(double),
			typeof(TransitionTarget),
			new FrameworkPropertyMetadata(DoubleBoxes.One, OnOpacityChanged));

	// TODO Uno: The private OpacityAnimation property (WUC opacity expression) is not ported.

	internal CompositeTransform? ClipTransform
	{
		get => (CompositeTransform?)GetValue(ClipTransformProperty);
		set => SetValue(ClipTransformProperty, value);
	}

	internal static DependencyProperty ClipTransformProperty { get; } =
		DependencyProperty.Register(
			nameof(ClipTransform),
			typeof(CompositeTransform),
			typeof(TransitionTarget),
			new FrameworkPropertyMetadata(null, OnClipTransformChanged));

	internal CompositeTransform? CompositeTransform
	{
		get => (CompositeTransform?)GetValue(CompositeTransformProperty);
		set => SetValue(CompositeTransformProperty, value);
	}

	internal static DependencyProperty CompositeTransformProperty { get; } =
		DependencyProperty.Register(
			nameof(CompositeTransform),
			typeof(CompositeTransform),
			typeof(TransitionTarget),
			new FrameworkPropertyMetadata(null, OnCompositeTransformChanged));

	internal Point TransformOrigin
	{
		get => (Point)GetValue(TransformOriginProperty);
		set => SetValue(TransformOriginProperty, value);
	}

	internal static DependencyProperty TransformOriginProperty { get; } =
		DependencyProperty.Register(
			nameof(TransformOrigin),
			typeof(Point),
			typeof(TransitionTarget),
			new FrameworkPropertyMetadata(default(Point), OnTransformOriginChanged));

	internal Point ClipTransformOrigin
	{
		get => (Point)GetValue(ClipTransformOriginProperty);
		set => SetValue(ClipTransformOriginProperty, value);
	}

	internal static DependencyProperty ClipTransformOriginProperty { get; } =
		DependencyProperty.Register(
			nameof(ClipTransformOrigin),
			typeof(Point),
			typeof(TransitionTarget),
			new FrameworkPropertyMetadata(default(Point), OnClipTransformOriginChanged));
}
