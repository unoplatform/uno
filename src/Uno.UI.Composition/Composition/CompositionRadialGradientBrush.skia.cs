#nullable enable

using SkiaSharp;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRadialGradientBrush : CompositionGradientBrush
	{
		private protected override void UpdatePaintCore(SKPaint paint, SKRect bounds)
		{
			var center = EllipseCenter.ToSKPoint();
			var gradientOrigin = EllipseCenter.ToSKPoint() + GradientOriginOffset.ToSKPoint();
			var radius = EllipseRadius.ToSKPoint();
			var transform = CreateTransformMatrix(bounds);

			// Transform the points into absolute coordinates.
			if (MappingMode == CompositionMappingMode.Relative)
			{
				// If the point are provided relative they must be multiplied by bounds.
				center.X *= bounds.Width;
				center.Y *= bounds.Height;

				gradientOrigin.X *= bounds.Width;
				gradientOrigin.Y *= bounds.Height;

				radius.X *= bounds.Width;
				radius.Y *= bounds.Height;
			}

			// Translate gradient points by bounds offset.
			center.X += bounds.Left;
			center.Y += bounds.Top;

			gradientOrigin.X += bounds.Left;
			gradientOrigin.Y += bounds.Top;
			//

			// SkiaSharp does not allow explicit definition of RadiusX and RadiusY.
			// Compute transformation matrix to compensate.
			ComputeRadiusAndScale(center, radius.X, radius.Y, out float gradientRadius, out SKMatrix matrix);

			// Clean up old shader
			if (paint.Shader != null)
			{
				paint.Shader.Dispose();
				paint.Shader = null;
			}

			if (gradientRadius > 0)
			{
				// Create radial gradient shader.
				SKShader shader =
					SKShader.CreateTwoPointConicalGradient(
						/* start */ gradientOrigin, /* start radius */ 0,
						/* end */ center, /* gradient radius */ gradientRadius,
						Colors, ColorPositions,
						TileMode, transform.PreConcat(matrix));

				paint.Shader = shader;
				paint.Color = SKColors.Black;
			}
			else
			{
				// With radius equal to 0, SkiaSharp does not draw anything.
				// But we expect last gradient color.

				// If there are no gradient stops available, use transparent.
				SKColor color = SKColors.Transparent;
				if (Colors!.Length > 0)
				{
					color = Colors[Colors.Length - 1];
				}

				double alpha = (color.Alpha / 255.0);
				paint.Color = color.WithAlpha((byte)(alpha * 255));
			}
		}

		private void ComputeRadiusAndScale(SKPoint center, float radiusX, float radiusY, out float radius, out SKMatrix matrix)
		{
			matrix = SKMatrix.Identity;
			if (radiusX == 0 || radiusY == 0)
			{
				// Handle this specific case as zero division would cause us troubles.
				radius = 0;
				return;
			}

			float scaleDownRatio;
			if (radiusX >= radiusY)
			{
				// radiusX is larger, use it and scale down radiusY.
				radius = radiusX;

				scaleDownRatio = radiusY / radiusX;

				SetScaleTranslate(
					ref matrix,
					/* scale x */ 1,
					/* scale y */ scaleDownRatio,
					/* translate x */ 0,
					/* translate y */ center.Y - scaleDownRatio * center.Y);
			}
			else
			{
				// radiusY is larger, use it and scale down radiusX.
				radius = radiusY;

				scaleDownRatio = radiusX / radiusY;

				SetScaleTranslate(
					ref matrix,
					/* scale x */ scaleDownRatio,
					/* scale y */ 1,
					/* translate x */ center.X - scaleDownRatio * center.X,
					/* translate y */ 0);
			}
		}

		private void SetScaleTranslate(ref SKMatrix matrix, float sx, float sy, float tx, float ty)
		{
			matrix.ScaleX = sx;
			matrix.SkewX = 0;
			matrix.TransX = tx;

			matrix.SkewY = 0;
			matrix.ScaleY = sy;
			matrix.TransY = ty;

			matrix.Persp0 = 0;
			matrix.Persp1 = 0;
			matrix.Persp2 = 1;
		}
	}
}
