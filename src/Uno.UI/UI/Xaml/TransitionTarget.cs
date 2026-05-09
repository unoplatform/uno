// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\core\inc\TransitionTarget.h, commit 978ab6363
// MUX Reference src\dxaml\xcp\components\elements\TransitionTarget.cpp, commit 978ab6363

#nullable enable

using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml;

/// <summary>
/// Provides an internal animation/transition wrapper exposed by <see cref="UIElement.TransitionTarget"/>.
/// It carries CompositeTransform, ClipTransform, Opacity and origin values that are layered on top
/// of the owning element's properties, mirroring the WinUI rendering pipeline.
/// </summary>
public sealed partial class TransitionTarget : DependencyObject
{
	private readonly UIElement? _owner;

	public TransitionTarget()
	{
	}

	internal TransitionTarget(UIElement owner)
	{
		_owner = owner;

		// CompositeTransform and ClipTransform are always present in WinUI's TransitionTarget so that
		// dependent property paths can resolve and animate.
		CompositeTransform = new CompositeTransform();
		ClipTransform = new CompositeTransform();
	}

	public CompositeTransform CompositeTransform
	{
		get => (CompositeTransform)GetValue(CompositeTransformProperty);
		set => SetValue(CompositeTransformProperty, value);
	}

	public static DependencyProperty CompositeTransformProperty { get; } =
		DependencyProperty.Register(
			nameof(CompositeTransform),
			typeof(CompositeTransform),
			typeof(TransitionTarget),
			new FrameworkPropertyMetadata(null, OnCompositeTransformChanged));

	private static void OnCompositeTransformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TransitionTarget tt && tt._owner is { } owner && e.NewValue is Transform newTransform)
		{
			owner.RenderTransform = newTransform;
		}
	}

	public CompositeTransform ClipTransform
	{
		get => (CompositeTransform)GetValue(ClipTransformProperty);
		set => SetValue(ClipTransformProperty, value);
	}

	public static DependencyProperty ClipTransformProperty { get; } =
		DependencyProperty.Register(
			nameof(ClipTransform),
			typeof(CompositeTransform),
			typeof(TransitionTarget),
			new FrameworkPropertyMetadata(null, OnClipTransformChanged));

	private static void OnClipTransformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TransitionTarget tt && tt._owner is { } owner && e.NewValue is CompositeTransform newTransform)
		{
			TransitionTargetClipManager.AttachClipTransform(owner, newTransform);
		}
	}

	public Point ClipTransformOrigin
	{
		get => (Point)GetValue(ClipTransformOriginProperty);
		set => SetValue(ClipTransformOriginProperty, value);
	}

	public static DependencyProperty ClipTransformOriginProperty { get; } =
		DependencyProperty.Register(
			nameof(ClipTransformOrigin),
			typeof(Point),
			typeof(TransitionTarget),
			new FrameworkPropertyMetadata(default(Point), OnClipTransformOriginChanged));

	private static void OnClipTransformOriginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TransitionTarget tt && tt._owner is { } owner && e.NewValue is Point origin)
		{
			TransitionTargetClipManager.UpdateClipOrigin(owner, origin);
		}
	}

	public double Opacity
	{
		get => (double)GetValue(OpacityProperty);
		set => SetValue(OpacityProperty, value);
	}

	public static DependencyProperty OpacityProperty { get; } =
		DependencyProperty.Register(
			nameof(Opacity),
			typeof(double),
			typeof(TransitionTarget),
			new FrameworkPropertyMetadata(1.0, OnOpacityChanged));

	private static void OnOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TransitionTarget tt && tt._owner is { } owner)
		{
			// WinUI multiplies UIElement.Opacity with TransitionTarget.Opacity at render time. Uno's renderer
			// does not yet honor TransitionTarget.Opacity directly, so we route the animated value to the
			// owner's Opacity. This loses the multiplicative behavior when the user has also set Opacity, but
			// matches the visible result for the typical animation case where the user starts at 1.0.
			owner.Opacity = (double)e.NewValue;
		}
	}

	public Point TransformOrigin
	{
		get => (Point)GetValue(TransformOriginProperty);
		set => SetValue(TransformOriginProperty, value);
	}

	public static DependencyProperty TransformOriginProperty { get; } =
		DependencyProperty.Register(
			nameof(TransformOrigin),
			typeof(Point),
			typeof(TransitionTarget),
			new FrameworkPropertyMetadata(default(Point), OnTransformOriginChanged));

	private static void OnTransformOriginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TransitionTarget tt && tt._owner is { } owner && e.NewValue is Point origin)
		{
			owner.RenderTransformOrigin = origin;
		}
	}
}
