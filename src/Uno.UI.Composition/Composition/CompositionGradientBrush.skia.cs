#nullable enable

using System.Numerics;
using SkiaSharp;

namespace Windows.UI.Composition
{
	public partial class CompositionGradientBrush
	{
		private bool _isColorStopsValid;

		private SKColor[]? _colors;
		private float[]? _colorPositions;
		private SKShaderTileMode _tileMode;

		private protected SKColor[]? Colors => _colors;
		private protected float[]? ColorPositions => _colorPositions;
		private protected SKShaderTileMode TileMode => _tileMode;

		internal override sealed void UpdatePaint(SKPaint paint, SKRect bounds)
		{
			if (!_isColorStopsValid)
			{
				UpdateColorStops(ColorStops);
			}

			UpdatePaintCore(paint, bounds);
		}

		private protected virtual void UpdatePaintCore(SKPaint paint, SKRect bounds)
		{

		}

		private protected SKMatrix CreateTransformMatrix(SKRect bounds)
		{
			var transform = SKMatrix.Identity;

			// Translate to origin
			if (CenterPoint != Vector2.Zero)
			{
				transform = SKMatrix.CreateTranslation(-CenterPoint.X, -CenterPoint.Y);
			}

			// Scaling
			if (Scale != Vector2.One)
			{
				transform = transform.PostConcat(SKMatrix.CreateScale(Scale.X, Scale.Y));
			}

			// Rotating
			if (RotationAngle != 0)
			{
				transform = transform.PostConcat(SKMatrix.CreateRotation(RotationAngle));
			}

			// Translating
			if (Offset != Vector2.Zero)
			{
				transform = transform.PostConcat(SKMatrix.CreateTranslation(Offset.X, Offset.Y));
			}

			// Translate back
			if (CenterPoint != Vector2.Zero)
			{
				transform = transform.PostConcat(SKMatrix.CreateTranslation(CenterPoint.X, CenterPoint.Y));
			}

			if (!TransformMatrix.IsIdentity)
			{
				transform = transform.PostConcat(TransformMatrix.ToSKMatrix());
			}

			var relativeTransform = RelativeTransformMatrix.IsIdentity ? SKMatrix.Identity : RelativeTransformMatrix.ToSKMatrix();
			if (!relativeTransform.IsIdentity)
			{
				relativeTransform.TransX *= bounds.Width;
				relativeTransform.TransY *= bounds.Height;

				transform = transform.PostConcat(relativeTransform);
			}

			return transform;
		}

		private void UpdateColorStops(CompositionColorGradientStopCollection colorStops)
		{
			var stopCount = colorStops.Count;
			var colors = _colors;
			var colorPositions = _colorPositions;

			if (colors == null || colors.Length != stopCount)
			{
				colors = new SKColor[stopCount];
				colorPositions = new float[stopCount];
			}

			for (int i = 0; i < colorStops.Count; i++)
			{
				var gradientStop = colorStops[i];

				colors[i] = gradientStop.Color.ToSKColor();
				colorPositions![i] = gradientStop.Offset;
			}

			_colors = colors;
			_colorPositions = colorPositions;
			_isColorStopsValid = true;
		}

		partial void OnColorStopsChanged(CompositionColorGradientStopCollection colorStops) => _isColorStopsValid = false;

		partial void OnExtendModeChanged(CompositionGradientExtendMode extendMode)
		{
			SKShaderTileMode tileMode;
			switch (extendMode)
			{
				default:
				case CompositionGradientExtendMode.Clamp:
					tileMode = SKShaderTileMode.Clamp;
					break;
				case CompositionGradientExtendMode.Mirror:
					tileMode = SKShaderTileMode.Mirror;
					break;
				case CompositionGradientExtendMode.Wrap:
					tileMode = SKShaderTileMode.Repeat;
					break;
			}

			_tileMode = tileMode;
		}
	}
}
