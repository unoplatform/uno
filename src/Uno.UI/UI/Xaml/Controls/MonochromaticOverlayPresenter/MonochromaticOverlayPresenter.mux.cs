// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference MonochromaticOverlayPresenter.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using System.Numerics;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class MonochromaticOverlayPresenter
{
	public MonochromaticOverlayPresenter()
	{
		SizeChanged += (s, e) =>
		{
			InvalidateBrush();
		};
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		InvalidateBrush();
	}

	private void InvalidateBrush()
	{
		// Delay brush updates until Tick to coalesce changes and avoid rebuilding the effect when we don't need to.
		if (!_needsBrushUpdate)
		{
			_needsBrushUpdate = true;
			var weakThis = new WeakReference<MonochromaticOverlayPresenter>(this);
			SharedHelpers.QueueCallbackForCompositionRendering(() =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.UpdateBrush();
					strongThis._needsBrushUpdate = false;
				}
			});
		}
	}

	private void UpdateBrush()
	{
		if (SourceElement is { } targetElement)
		{
			var newColor = ReplacementColor;
			if (_replacementColor != newColor)
			{
				_replacementColor = newColor;
				_effectFactory = null;
			}

			var compositor = Microsoft.UI.Xaml.Media.CompositionTarget.GetCompositorForCurrentThread();

			if (_effectFactory is null)
			{
				// Build an effect that takes the source image and uses the alpha channel and replaces all other channels with
				// the ReplacementColor's RGB.
				var colorMatrixEffect = new ColorMatrixEffect();
				colorMatrixEffect.Source = new CompositionEffectSourceParameter("source");
				var colorMatrix = new Matrix5x4();

				// If the ReplacementColor is not transparent then use the RGB values as the new color. Otherwise
				// just show the target by using an Identity colorMatrix.
				if (_replacementColor.A != 0)
				{
					colorMatrix.M51 = (float)(_replacementColor.R / 255.0);
					colorMatrix.M52 = (float)(_replacementColor.G / 255.0);
					colorMatrix.M53 = (float)(_replacementColor.B / 255.0);
					colorMatrix.M44 = 1;
				}
				else
				{
					colorMatrix.M11 = colorMatrix.M22 = colorMatrix.M33 = colorMatrix.M44 = 1;
				}
				colorMatrixEffect.ColorMatrix = colorMatrix;

				_effectFactory = compositor.CreateEffectFactory(colorMatrixEffect);
			}

			var actualSize = new Vector2((float)ActualWidth, (float)ActualHeight);
			var transform = TransformToVisual(targetElement);
			var offset = transform.TransformPoint(new Point(0, 0));

			// Create a VisualSurface positioned at the same location as this control and feed that
			// through the color effect.
			var surfaceBrush = compositor.CreateSurfaceBrush();
			surfaceBrush.Stretch = CompositionStretch.None;
			var surface = compositor.CreateVisualSurface();

			// Select the source visual and the offset/size of this control in that element's space.
			surface.SourceVisual = ElementCompositionPreview.GetElementVisual(targetElement);
			surface.SourceOffset = new Vector2((float)offset.X, (float)offset.Y);
			surface.SourceSize = actualSize;
			surfaceBrush.Surface = surface;
			surfaceBrush.Stretch = CompositionStretch.None;
			var compBrush = _effectFactory.CreateBrush()!;
			compBrush.SetSourceParameter("source", surfaceBrush);

			var visual = compositor.CreateSpriteVisual();
			visual.Size = actualSize;
			visual.Brush = compBrush;

			ElementCompositionPreview.SetElementChildVisual(this, visual);
		}
	}
}
