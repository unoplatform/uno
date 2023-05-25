#nullable enable

using System.Numerics;
using SkiaSharp;

namespace Windows.UI.Composition
{
	public partial class ShapeVisual
	{
		internal override void Render(SKSurface surface)
		{
			foreach (var shape in Shapes)
			{
				var shapeTransform = shape.TransformMatrix;
				if (shape.Scale != Vector2.One)
				{
					shapeTransform *= Matrix3x2.CreateScale(shape.Scale, shape.CenterPoint);
				}

				if (shape.RotationAngle is not 0)
				{
					shapeTransform *= Matrix3x2.CreateRotation(shape.RotationAngle, shape.CenterPoint);
				}

				if (!shapeTransform.IsIdentity)
				{
					var shapeTransformMatrix = shapeTransform.ToSKMatrix44().Matrix;

					surface.Canvas.Save();
					surface.Canvas.Concat(ref shapeTransformMatrix);

					shape.Render(surface);

					surface.Canvas.Restore();
				}
				else
				{
					shape.Render(surface);
				}
			}
		}
	}
}
