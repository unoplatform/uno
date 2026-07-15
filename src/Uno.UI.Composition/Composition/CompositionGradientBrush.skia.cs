#nullable enable

using System.Numerics;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.UI.Composition
{
	public partial class CompositionGradientBrush
	{
		private bool _isColorStopsValid;

		private float[]? _colorPositions;

		private protected float[]? ColorPositions => _colorPositions;

		internal override bool CanPaint() => true;

		internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
		{
			if (!_isColorStopsValid)
			{
				UpdateColorStops(ColorStops);
			}

			if (!TryBuildShader(bounds, out var shader) || shader is null)
			{
				// This gradient kind can't build a neutral shader; nothing is painted.
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

		// Row-vector convention: `a * b` applies `a` first then `b`, matching SKMatrix.PostConcat's
		// "apply after" semantics used previously.
		private protected Matrix3x2 CreateTransformMatrix(Rect bounds)
		{
			var transform = Matrix3x2.Identity;

			// Translate to origin
			if (CenterPoint != Vector2.Zero)
			{
				transform = Matrix3x2.CreateTranslation(-CenterPoint.X, -CenterPoint.Y);
			}

			// Scaling
			if (Scale != Vector2.One)
			{
				transform *= Matrix3x2.CreateScale(Scale.X, Scale.Y);
			}

			// Rotating
			if (RotationAngle != 0)
			{
				transform *= Matrix3x2.CreateRotation(RotationAngle);
			}

			// Translating
			if (Offset != Vector2.Zero)
			{
				transform *= Matrix3x2.CreateTranslation(Offset.X, Offset.Y);
			}

			// Translate back
			if (CenterPoint != Vector2.Zero)
			{
				transform *= Matrix3x2.CreateTranslation(CenterPoint.X, CenterPoint.Y);
			}

			if (!TransformMatrix.IsIdentity)
			{
				transform *= TransformMatrix;
			}

			var relativeTransform = RelativeTransformMatrix;
			if (!relativeTransform.IsIdentity)
			{
				relativeTransform.M31 *= (float)bounds.Width;
				relativeTransform.M32 *= (float)bounds.Height;

				transform *= relativeTransform;
			}

			return transform;
		}

		private void UpdateColorStops(CompositionColorGradientStopCollection colorStops)
		{
			var stopCount = colorStops.Count;
			var colorPositions = _colorPositions;

			if (colorPositions is null || colorPositions.Length != stopCount)
			{
				colorPositions = new float[stopCount];
			}

			for (int i = 0; i < colorStops.Count; i++)
			{
				colorPositions[i] = colorStops[i].Offset;
			}

			_colorPositions = colorPositions;
			_isColorStopsValid = true;
		}

		partial void OnColorStopsChanged(CompositionColorGradientStopCollection colorStops) => _isColorStopsValid = false;
	}
}
