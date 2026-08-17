#nullable enable

using Uno.UI.Composition.Drawing;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Uno.UI.Composition;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

public partial class ShapeVisual
{
	private bool _needsContinuousUpdates;

	/// <inheritdoc />
	internal override IGeometry? Paint(in PaintingSession session)
	{
		if (Size.X == 0 || Size.Y == 0)
		{
			return null;
		}

		// TODO: ShapeVisuals should be clipping to the size rect. However, this breaks shapes for us because
		// we implement them with ShapeVisuals and they don't clip anything. The problem is that
		// the WinUI implementation doesn't use ShapeVisuals for shapes, but a combination of ContainerVisuals and
		// SpriteVisuals. When_StrokeThickness_Is_GreaterThan_Or_Equals_Width and
		// When_Border_CornerRadius_HitTesting fail when you uncomment the following line.
		// session.Session.ClipRect(new Rect(0, 0, Size.X, Size.Y));

		// TODO: ViewBox.Stretch, ViewBox.HorizontalAlignmentRatio and ViewBox.VerticalAlignmentRatio
		if (ViewBox is not null)
		{
			session.Session.Scale(
				ViewBox.Size.X > 0 ? Size.X / ViewBox.Size.X : 1,
				ViewBox.Size.Y > 0 ? Size.Y / ViewBox.Size.Y : 1);
			session.Session.Translate(-ViewBox.Offset.X, -ViewBox.Offset.Y); // translate before scaling
		}

		if (_shapes is { Count: not 0 } shapes)
		{
			for (var i = 0; i < shapes.Count; i++)
			{
				shapes[i].Render(in session);
			}
		}

		base.Paint(in session);

		return BuildOwnContentPath();
	}

	// The geometry this ShapeVisual's shapes cover, in local space, for a precise per-visual damage region.
	// Null when any child isn't an analytically-describable sprite shape, so damage falls back to bounds.
	private IGeometry? BuildOwnContentPath()
	{
		if (_shapes is not { Count: > 0 } shapes)
		{
			return null;
		}

		IGeometry? dst = null;
		for (var i = 0; i < shapes.Count; i++)
		{
			if (shapes[i] is not CompositionSpriteShape sprite)
			{
				dst?.Dispose();
				return null;
			}

			if (sprite.BuildRenderGeometry() is { } g)
			{
				if (dst is null)
				{
					dst = g;
				}
				else
				{
					var previous = dst;
					dst = dst.Combine(g, GeometryCombineMode.Union);
					previous.Dispose();
					g.Dispose();
				}
			}
		}

		if (dst is null)
		{
			return null;
		}

		if (ViewBox is { } viewBox && viewBox.Size.X > 0 && viewBox.Size.Y > 0)
		{
			var sx = Size.X / viewBox.Size.X;
			var sy = Size.Y / viewBox.Size.Y;
			// Match Paint's canvas transform (Scale then Translate): a local point is translated by -Offset then scaled.
			var m = Matrix3x2.CreateTranslation(-viewBox.Offset.X, -viewBox.Offset.Y) * Matrix3x2.CreateScale(sx, sy);
			var previous = dst;
			dst = dst.Transform(m);
			previous.Dispose();
		}

		return dst;
	}

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		base.OnPropertyChangedCore(propertyName, isSubPropertyChange);
		if (propertyName == nameof(Shapes))
		{
			_needsContinuousUpdates = _shapes?.OfType<CompositionSpriteShape>().Any(s => s.FillBrush?.RequiresRepaintOnEveryFrame ?? false) ?? false;
		}
	}

	internal override bool RequiresRepaintOnEveryFrame => _needsContinuousUpdates;

	internal override bool CanPaint() => base.CanPaint() || (_shapes?.Any(s => s.CanPaint()) ?? false);

	/// <remarks>This does NOT take the clipping into account.</remarks>
	internal override bool HitTest(Point point)
	{
		if (_shapes is null)
		{
			return false;
		}

		foreach (var shape in _shapes)
		{
			if (shape.HitTest(point))
			{
				return true;
			}
		}

		// Do not check the child visuals. On WinUI, if you add a child visual (e.g. using ContainerVisual.Children.InsertAtTop),
		// the child doesn't factor at all in hit-testing. The children of the UIElement that owns this visual will be checked
		// separately in VisualTreeHelper.HitTest

		return false;
	}
}
