using System.Numerics;
using SkiaSharp;

namespace Windows.UI.Composition
{
	public partial class ShapeVisual
	{
		internal override void Render(SKSurface surface, SKImageInfo info)
		{
			foreach(var shape in Shapes)
			{
				surface.Canvas.Save();

				var visualMatrix = surface.Canvas.TotalMatrix;

				visualMatrix = visualMatrix.PreConcat(SKMatrix.CreateTranslation(shape.Offset.X, shape.Offset.Y));

				if (shape.Scale != new Vector2(1, 1))
				{
					visualMatrix = visualMatrix.PreConcat(SKMatrix.CreateScale(shape.Scale.X, shape.Scale.Y));
				}

				if (shape.RotationAngleInDegrees != 0)
				{
					visualMatrix = visualMatrix.PreConcat(SKMatrix.CreateRotationDegrees(shape.RotationAngleInDegrees, shape.CenterPoint.X, shape.CenterPoint.Y));
				}

				if (shape.TransformMatrix != Matrix3x2.Identity)
				{
					visualMatrix = visualMatrix.PreConcat(shape.TransformMatrix.ToSKMatrix44().Matrix);
				}

				surface.Canvas.SetMatrix(visualMatrix);

				shape.Render(surface, info);

				surface.Canvas.Restore();
			}
		}
	}
}
