#nullable enable

using System;
using SkiaSharp;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

public partial class CompositionContainerShape
{
	internal override void Paint(in Visual.PaintingSession session)
	{
		if (_shapes is { Count: > 0 } shapes)
		{
			// Apply the container's transform once, then render children. CompositionShape.Render()
			// already takes care of applying child Offset/transform on top of this.
			var transform = CombinedTransformMatrix;
			var hasTransform = !transform.IsIdentity;

			if (hasTransform)
			{
				session.Canvas.Save();

				SKMatrix m = new SKMatrix
				{
					ScaleX = transform.M11,
					SkewY = transform.M12,
					SkewX = transform.M21,
					ScaleY = transform.M22,
					TransX = transform.M31,
					TransY = transform.M32,
					Persp2 = 1f,
				};

				session.Canvas.Concat(in m);
			}

			for (var i = 0; i < shapes.Count; i++)
			{
				shapes[i].Render(in session);
			}

			if (hasTransform)
			{
				session.Canvas.Restore();
			}
		}
	}

	internal override bool CanPaint()
	{
		if (_shapes is { Count: > 0 } shapes)
		{
			for (var i = 0; i < shapes.Count; i++)
			{
				if (shapes[i].CanPaint())
				{
					return true;
				}
			}
		}

		return false;
	}

	internal override bool HitTest(Point point)
	{
		if (_shapes is { Count: > 0 } shapes)
		{
			for (var i = 0; i < shapes.Count; i++)
			{
				if (shapes[i].HitTest(point))
				{
					return true;
				}
			}
		}

		return false;
	}
}
