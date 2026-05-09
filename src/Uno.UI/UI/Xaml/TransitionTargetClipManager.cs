// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml;

// Mirrors the rendering side of CTransitionTarget::ApplyClip: the clip transform is applied to a
// rectangle covering the target's bounds. WinUI does this via the compositor; Uno installs an actual
// RectangleGeometry Clip on the owner so the public clip property paths animate something visible.
internal static class TransitionTargetClipManager
{
	public static void AttachClipTransform(UIElement owner, CompositeTransform clipTransform)
	{
		var rectGeom = EnsureRectangleClip(owner);
		rectGeom.Transform = clipTransform;
		ApplyOrigin(owner, rectGeom, clipTransform);
	}

	public static void UpdateClipOrigin(UIElement owner, Point origin)
	{
		if (owner.Clip is RectangleGeometry rectGeom && rectGeom.Transform is CompositeTransform clipTransform)
		{
			ApplyOrigin(owner, rectGeom, clipTransform, origin);
		}
	}

	private static RectangleGeometry EnsureRectangleClip(UIElement owner)
	{
		if (owner.Clip is not RectangleGeometry existing)
		{
			existing = new RectangleGeometry();
			owner.Clip = existing;
			SyncClipRect(owner, existing);

			if (owner is FrameworkElement fe)
			{
				fe.SizeChanged -= OnOwnerSizeChanged;
				fe.SizeChanged += OnOwnerSizeChanged;
			}
		}
		else
		{
			SyncClipRect(owner, existing);
		}
		return existing;
	}

	private static void OnOwnerSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (sender is UIElement owner && owner.Clip is RectangleGeometry rg)
		{
			SyncClipRect(owner, rg);
			if (owner.GetTransitionTargetOrNull() is { ClipTransformOrigin: var origin } &&
				rg.Transform is CompositeTransform clipTransform)
			{
				ApplyOrigin(owner, rg, clipTransform, origin);
			}
		}
	}

	private static void SyncClipRect(UIElement owner, RectangleGeometry geometry)
	{
		var size = owner.RenderSize;
		if (size.Width <= 0 || size.Height <= 0)
		{
			if (owner is FrameworkElement fe)
			{
				size = new Size(fe.ActualWidth, fe.ActualHeight);
			}
		}

		geometry.Rect = new Rect(0, 0, size.Width, size.Height);
	}

	private static void ApplyOrigin(UIElement owner, RectangleGeometry rectGeom, CompositeTransform clipTransform)
	{
		if (owner.GetTransitionTargetOrNull() is { ClipTransformOrigin: var origin })
		{
			ApplyOrigin(owner, rectGeom, clipTransform, origin);
		}
	}

	private static void ApplyOrigin(UIElement owner, RectangleGeometry rectGeom, CompositeTransform clipTransform, Point origin)
	{
		clipTransform.CenterX = origin.X * rectGeom.Rect.Width;
		clipTransform.CenterY = origin.Y * rectGeom.Rect.Height;
	}
}
