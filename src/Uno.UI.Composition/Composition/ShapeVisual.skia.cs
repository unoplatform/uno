#nullable enable

using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using SkiaSharp;
using Uno.UI.Composition;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

public partial class ShapeVisual
{
	private bool _needsContinuousUpdates;

	/// <inheritdoc />
	internal override void Paint(in PaintingSession session)
	{
		var canvas = session.Canvas;

		if (Size.X == 0 || Size.Y == 0)
		{
			return;
		}

		// TODO: ShapeVisuals should be clipping to the size rect. However, this breaks shapes for us because
		// we implement them with ShapeVisuals and they don't clip anything. The problem is that
		// the WinUI implementation doesn't use ShapeVisuals for shapes, but a combination of ContainerVisuals and
		// SpriteVisuals. When_StrokeThickness_Is_GreaterThan_Or_Equals_Width and
		// When_Border_CornerRadius_HitTesting fail when you uncomment the following line.
		// canvas.ClipRect(new SKRect(0, 0, Size.X, Size.Y));

		// TODO: ViewBox.Stretch, ViewBox.HorizontalAlignmentRatio and ViewBox.VerticalAlignmentRatio
		if (ViewBox is not null)
		{
			canvas.Scale(
				ViewBox.Size.X > 0 ? Size.X / ViewBox.Size.X : 1,
				ViewBox.Size.Y > 0 ? Size.Y / ViewBox.Size.Y : 1);
			canvas.Translate(-ViewBox.Offset.X, -ViewBox.Offset.Y); // translate before scaling
		}

		if (_shapes is { Count: not 0 } shapes)
		{
			for (var i = 0; i < shapes.Count; i++)
			{
				shapes[i].Render(in session);
			}
		}

		base.Paint(in session);
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
