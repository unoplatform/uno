#nullable enable

using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using SkiaSharp;
using Uno.UI.Composition;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

public partial class ShapeVisual
{
	private GCHandle _gcHandle;

	partial void InitializePartial()
	{
		_gcHandle = GCHandle.Alloc(this, GCHandleType.Weak);
		Handle = GCHandle.ToIntPtr(_gcHandle);
	}

	internal IntPtr Handle { get; private set; }

	internal WeakReference? Owner { get; set; }

	internal override SKPath? GetPrePaintingClipping()
		=> GetViewBoxPathInElementCoordinateSpace() is { } path
			? base.GetPrePaintingClipping() is { } baseClip
				? path.Op(baseClip, SKPathOp.Intersect)
				: path
			: base.GetPrePaintingClipping();

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

	internal SKPath? GetViewBoxPathInElementCoordinateSpace()
	{
		if (ViewBox is not { } viewBox)
		{
			return null;
		}

		var shape = new SKPath();
		var clipRect = new SKRect(viewBox.Offset.X, viewBox.Offset.Y, viewBox.Offset.X + viewBox.Size.X, viewBox.Offset.Y + viewBox.Size.Y);
		shape.AddRect(clipRect);
		if (viewBox.IsAncestorClip)
		{
			Matrix4x4.Invert(TotalMatrix, out var totalMatrixInverted);
			var childToParentTransform = Parent!.TotalMatrix * totalMatrixInverted;
			if (!childToParentTransform.IsIdentity)
			{

				shape.Transform(childToParentTransform.ToSKMatrix());
			}
		}

		return shape;
	}

	internal override bool CanPaint => base.CanPaint || (_shapes?.Any(s => s.CanPaint) ?? false);

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
