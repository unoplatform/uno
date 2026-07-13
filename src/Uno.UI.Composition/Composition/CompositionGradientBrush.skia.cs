#nullable enable

using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.UI.Composition
{
	public partial class CompositionGradientBrush
	{
		private static readonly SKPaint _tempPaint = new();
		private bool _isColorStopsValid;

		private SKColor[]? _colors;
		private float[]? _colorPositions;
		private SKShaderTileMode _tileMode;

		private protected SKColor[]? Colors => _colors;
		private protected float[]? ColorPositions => _colorPositions;
		private protected SKShaderTileMode TileMode => _tileMode;

		internal override bool CanPaint() => true;

		internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
		{
			if (!_isColorStopsValid)
			{
				UpdateColorStops(ColorStops);
			}
			var (shader, color) = GetPaintingParameters(bounds);
			_tempPaint.Reset();
			_tempPaint.IsAntialias = true;
			_tempPaint.Shader = shader;
			_tempPaint.Color = color;
			_tempPaint.ColorFilter = opacity.ToColorFilter();
			canvas.DrawRect(bounds, _tempPaint);
		}

		internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
		{
			if (!_isColorStopsValid)
			{
				UpdateColorStops(ColorStops);
			}

			if (!TryBuildShader(bounds, out var shader) || shader is null)
			{
				// This gradient kind hasn't been migrated to the neutral factory yet; fall back to SKCanvas.
				return false;
			}

			session.DrawRect(bounds, new PaintParams(global::Windows.UI.Colors.Black)
			{
				IsAntialias = true,
				Shader = shader,
				ColorFilter = DrawingBackend.Current.CreateOpacityColorFilter(opacity),
			});
			return true;
		}

		/// <summary>Builds this gradient's shader through the backend factory. Default returns false (not migrated).</summary>
		private protected virtual bool TryBuildShader(Rect bounds, out IShader? shader)
		{
			shader = null;
			return false;
		}

		/// <summary>The gradient stop colors as backend-neutral colors, in stop order.</summary>
		private protected Color[] GetNeutralColors()
		{
			var stops = ColorStops;
			var colors = new Color[stops.Count];
			for (var i = 0; i < stops.Count; i++)
			{
				colors[i] = stops[i].Color;
			}
			return colors;
		}

		private protected GradientTileMode NeutralTileMode => ExtendMode switch
		{
			CompositionGradientExtendMode.Mirror => GradientTileMode.Mirror,
			CompositionGradientExtendMode.Wrap => GradientTileMode.Repeat,
			_ => GradientTileMode.Clamp,
		};

		private protected virtual (SKShader? shader, SKColor color) GetPaintingParameters(SKRect bounds) => (null, SKColors.Transparent);

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
