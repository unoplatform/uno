#nullable enable

using System;
using System.Numerics;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRadialGradientBrush : CompositionGradientBrush
	{
		private protected override bool TryBuildShader(Rect bounds, float opacity, out IShader? shader)
		{
			var center = EllipseCenter;
			var gradientOrigin = GradientOriginOffset;
			var radius = EllipseRadius;
			var transform = CreateTransformMatrix(bounds);

			if (MappingMode == CompositionMappingMode.Relative)
			{
				center.X *= (float)bounds.Width;
				center.Y *= (float)bounds.Height;
				gradientOrigin.X *= (float)bounds.Width;
				gradientOrigin.Y *= (float)bounds.Height;
				radius.X *= (float)bounds.Width;
				radius.Y *= (float)bounds.Height;
			}

			center.X += (float)bounds.Left;
			center.Y += (float)bounds.Top;
			gradientOrigin.X += (float)bounds.Left;
			gradientOrigin.Y += (float)bounds.Top;

			ComputeRadiusAndScale(center, radius.X, radius.Y, out var gradientRadius, out var matrix);

			var backend = DrawingBackend.Current;
			var tileMode = NeutralTileMode;
			var positions = ColorPositions!;

			if (gradientRadius > 0)
			{
				// The scale-down matrix is applied before the brush transform (SKMatrix.PreConcat), which in
				// the row-vector convention is `matrix * transform`.
				var totalMatrix = matrix * transform;
				if (EllipseCenter == GradientOriginOffset)
				{
					shader = backend.CreateRadialGradientShader(
						center, gradientRadius, GetNeutralColors(opacity), positions, tileMode, totalMatrix);
				}
				else
				{
					var colors = GetNeutralColors(opacity);
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

					Matrix3x2.Invert(totalMatrix, out var inverse);
					var origin = Vector2.Transform(gradientOrigin, inverse);

					var conical = backend.CreateTwoPointConicalGradientShader(
						center, gradientRadius, origin, 0,
						reversedColors, reversedPositions, tileMode, totalMatrix);
					shader = backend.ComposeShaders(backend.CreateColorShader(GetLastColorOrTransparentNeutral(opacity)), conical);
				}
			}
			else
			{
				// Radius 0: match the last gradient color everywhere (a color shader, modulated by opacity).
				shader = backend.CreateColorShader(GetLastColorOrTransparentNeutral(opacity));
			}

			return true;
		}

		private global::Windows.UI.Color GetLastColorOrTransparentNeutral(float opacity)
		{
			var colors = GetNeutralColors(opacity);
			return colors.Length > 0 ? colors[colors.Length - 1] : global::Windows.UI.Colors.Transparent;
		}

		// SkiaSharp doesn't allow explicit RadiusX/RadiusY on a radial gradient, so we build a scale-down
		// transform that squashes the larger axis onto the smaller and use a single radius.
		private void ComputeRadiusAndScale(Vector2 center, float radiusX, float radiusY, out float radius, out Matrix3x2 matrix)
		{
			matrix = Matrix3x2.Identity;
			if (radiusX == 0 || radiusY == 0)
			{
				// Handle this specific case as zero division would cause us troubles.
				radius = 0;
				return;
			}

			if (radiusX >= radiusY)
			{
				// radiusX is larger, use it and scale down radiusY.
				radius = radiusX;
				var scaleDownRatio = radiusY / radiusX;
				matrix = new Matrix3x2(1, 0, 0, scaleDownRatio, 0, center.Y - scaleDownRatio * center.Y);
			}
			else
			{
				// radiusY is larger, use it and scale down radiusX.
				radius = radiusY;
				var scaleDownRatio = radiusX / radiusY;
				matrix = new Matrix3x2(scaleDownRatio, 0, 0, 1, center.X - scaleDownRatio * center.X, 0);
			}
		}
	}
}
