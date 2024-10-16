#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class ShapeVisual
{
	[ThreadStatic] // safety
	private static SKPath? _prePaintingClipPath;

	private protected override void ApplyPrePaintingClipping(in SKCanvas canvas)
	{
		base.ApplyPrePaintingClipping(in canvas);
		_prePaintingClipPath ??= new SKPath();
		if (GetViewBoxPathInElementCoordinateSpace(_prePaintingClipPath))
		{
			canvas.ClipPath(_prePaintingClipPath, antialias: true);
		}
	}

	/// <inheritdoc />
	internal override void Paint(in PaintingSession session)
	{
		if (_shapes is { Count: not 0 } shapes)
		{
			for (var i = 0; i < shapes.Count; i++)
			{
				shapes[i].Render(in session);
			}
		}

		base.Paint(in session);
	}

	/// <returns>The same <see cref="dst"/> object.</returns>
	internal bool GetViewBoxPathInElementCoordinateSpace(SKPath dst)
	{
		if (ViewBox is not { } viewBox)
		{
			return false;
		}

		dst.Rewind();
		var clipRect = new SKRect(viewBox.Offset.X, viewBox.Offset.Y, viewBox.Offset.X + viewBox.Size.X, viewBox.Offset.Y + viewBox.Size.Y);
		dst.AddRect(clipRect);
		if (viewBox.IsAncestorClip)
		{
			Matrix4x4.Invert(TotalMatrix, out var totalMatrixInverted);
			var childToParentTransform = Parent!.TotalMatrix * totalMatrixInverted;
			if (!childToParentTransform.IsIdentity)
			{
				dst.Transform(childToParentTransform.ToSKMatrix());
			}
		}

		return true;
	}

	/// <remarks>This does NOT take the clipping into account.</remarks>
	internal virtual bool HitTest(Point point)
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
