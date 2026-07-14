#nullable enable

using System;
using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRadialGradientBrush : CompositionGradientBrush
	{
		// Neutral path: builds the shader through the backend factory (SKMatrix used only as internal math,
		// converted to Matrix3x2 at the boundary — same pattern as the linear brush).
		private protected override bool TryBuildShader(Rect bounds, out IShader? shader)
		{
			var skBounds = bounds.ToSKRect();
			var center = EllipseCenter.ToSKPoint();
			var gradientOrigin = GradientOriginOffset.ToSKPoint();
			var radius = EllipseRadius.ToSKPoint();
			var transform = CreateTransformMatrix(skBounds);

			if (MappingMode == CompositionMappingMode.Relative)
			{
				center.X *= skBounds.Width;
				center.Y *= skBounds.Height;
				gradientOrigin.X *= skBounds.Width;
				gradientOrigin.Y *= skBounds.Height;
				radius.X *= skBounds.Width;
				radius.Y *= skBounds.Height;
			}

			center.X += skBounds.Left;
			center.Y += skBounds.Top;
			gradientOrigin.X += skBounds.Left;
			gradientOrigin.Y += skBounds.Top;

			ComputeRadiusAndScale(center, radius.X, radius.Y, out var gradientRadius, out var matrix);

			var backend = DrawingBackend.Current;
			var tileMode = NeutralTileMode;
			var positions = ColorPositions!;

			if (gradientRadius > 0)
			{
				if (EllipseCenter == GradientOriginOffset)
				{
					shader = backend.CreateRadialGradientShader(
						new Vector2(center.X, center.Y), gradientRadius, GetNeutralColors(), positions, tileMode,
						transform.PreConcat(matrix).ToMatrix3x2());
				}
				else
				{
					var colors = GetNeutralColors();
					var reversedColors = new global::Windows.UI.Color[colors.Length];
					for (var i = 0; i < reversedColors.Length; i++)
					{
						reversedColors[i] = colors[colors.Length - 1 - i];
					}

					var reversedPositions = new float[positions.Length];
					for (var i = 0; i < positions.Length; i++)
					{
						var p = positions[i];
						reversedPositions[i] = (p > 0 && p < 1) ? Math.Abs(1 - p) : p;
					}

					var totalMatrix = transform.PreConcat(matrix);
					var origin = totalMatrix.Invert().MapPoint(gradientOrigin);

					var conical = backend.CreateTwoPointConicalGradientShader(
						new Vector2(center.X, center.Y), gradientRadius, new Vector2(origin.X, origin.Y), 0,
						reversedColors, reversedPositions, tileMode, totalMatrix.ToMatrix3x2());
					shader = backend.ComposeShaders(backend.CreateColorShader(GetLastColorOrTransparentNeutral()), conical);
				}
			}
			else
			{
				// Radius 0: match the last gradient color everywhere (a color shader, modulated by opacity).
				shader = backend.CreateColorShader(GetLastColorOrTransparentNeutral());
			}

			return true;
		}

		private global::Windows.UI.Color GetLastColorOrTransparentNeutral()
		{
			var colors = GetNeutralColors();
			return colors.Length > 0 ? colors[colors.Length - 1] : global::Windows.UI.Colors.Transparent;
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
