#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Composition;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Element visual of <see cref="ListViewBaseItemPresenter"/>: a <see cref="BorderVisual"/> that also paints the
/// chrome's under-content layer shapes, around the background that carries the Inline state fill.
/// </summary>
/// <remarks>Uno-specific: WinUI draws the under-content layer in the render walk (ListViewBaseItemChrome.cpp, RenderLayer).</remarks>
internal sealed class ListViewBaseItemChromeVisual(Compositor compositor) : BorderVisual(compositor)
{
	private static readonly List<CompositionSpriteShape> s_noShapes = new();

	private List<CompositionSpriteShape> _underContentShapes = s_noShapes;

	// Shapes before this index paint below the BorderVisual background, the others above it.
	private int _backgroundIndex;

	private int _underContentShapesVersion;

	internal IReadOnlyList<CompositionSpriteShape> UnderContentShapes => _underContentShapes;

	internal void SetUnderContentShapes(List<CompositionSpriteShape>? shapes, int backgroundIndex)
	{
		shapes ??= s_noShapes;
		if (shapes.Count == 0 && _underContentShapes.Count == 0)
		{
			return;
		}

		foreach (var shape in _underContentShapes)
		{
			OnCompositionPropertyChanged(shape, null, nameof(UnderContentShapes));
			ReleaseShapeGeometry(shape);
		}

		_underContentShapes = shapes;
		_backgroundIndex = Math.Clamp(backgroundIndex, 0, shapes.Count);

		foreach (var shape in _underContentShapes)
		{
			// Brush changes on a shape invalidate this visual.
			OnCompositionPropertyChanged(null, shape, nameof(UnderContentShapes));
		}

		SetProperty(ref _underContentShapesVersion, _underContentShapesVersion + 1, nameof(UnderContentShapes));
	}

	internal static void ReleaseShapeGeometry(CompositionSpriteShape shape)
	{
		if (shape.Geometry is CompositionPathGeometry geometry)
		{
			SetPath(geometry, null);
		}
	}

	internal override void Paint(in PaintingSession session)
	{
		var shapes = _underContentShapes;

		for (var i = 0; i < _backgroundIndex; i++)
		{
			shapes[i].Render(in session);
		}

		base.Paint(in session);

		for (var i = _backgroundIndex; i < shapes.Count; i++)
		{
			shapes[i].Render(in session);
		}
	}

	internal override bool CanPaint()
	{
		if (base.CanPaint())
		{
			return true;
		}

		foreach (var shape in _underContentShapes)
		{
			if (shape.CanPaint())
			{
				return true;
			}
		}

		return false;
	}

	internal override bool RequiresRepaintOnEveryFrame
	{
		get
		{
			if (base.RequiresRepaintOnEveryFrame)
			{
				return true;
			}

			foreach (var shape in _underContentShapes)
			{
				if (shape.FillBrush?.RequiresRepaintOnEveryFrame ?? false)
				{
					return true;
				}
			}

			return false;
		}
	}

	internal override float DamageRegionSamplingMargin
	{
		get
		{
			var margin = base.DamageRegionSamplingMargin;
			foreach (var shape in _underContentShapes)
			{
				margin = Math.Max(margin, shape.FillBrush?.DamageRegionSamplingMargin ?? 0);
			}

			return margin;
		}
	}

	internal override bool HitTest(Point point)
	{
		if (base.HitTest(point))
		{
			return true;
		}

		foreach (var shape in _underContentShapes)
		{
			if (shape.HitTest(point))
			{
				return true;
			}
		}

		return false;
	}

	private protected override bool TryAddShadowPaths(List<(IGeometry path, float alpha)> output)
		// The chrome shapes are not described analytically.
		=> _underContentShapes.Count == 0 && base.TryAddShadowPaths(output);
}
