#nullable enable

using System;
using System.Numerics;
using Uno.Extensions;
using Uno.UI.Composition;
using SkiaSharp;

namespace Microsoft.UI.Composition
{
	public partial class CompositionGradientBrush : CompositionBrush, I2DTransformableObject
	{
		private CompositionGradientExtendMode _extendMode;
		private CompositionMappingMode _mappingMode;
		private Matrix3x2 _transformMatrix = Matrix3x2.Identity;
		private Matrix3x2 _relativeTransformMatrix = Matrix3x2.Identity;
		private Vector2 _scale = new Vector2(1, 1);
		private float _rotationAngle;
		private Vector2 _offset;
		private Vector2 _centerPoint;

		internal CompositionGradientBrush(Compositor compositor)
			: base(compositor)
		{
			ColorStops = new CompositionColorGradientStopCollection(this);
		}

		public CompositionColorGradientStopCollection ColorStops { get; }

		public CompositionGradientExtendMode ExtendMode
		{
			get => _extendMode;
			set => SetEnumProperty(ref _extendMode, value);
		}

		public CompositionMappingMode MappingMode
		{
			get => _mappingMode;
			set => SetEnumProperty(ref _mappingMode, value);
		}

		public Matrix3x2 TransformMatrix
		{
			get => _transformMatrix;
			set => SetProperty(ref _transformMatrix, value);
		}

		public Vector2 Scale
		{
			get => _scale;
			set => SetProperty(ref _scale, value);
		}

		public float RotationAngleInDegrees
		{
			get => (float)MathEx.ToDegree(_rotationAngle);
			set => RotationAngle = (float)MathEx.ToRadians(value);
		}

		public float RotationAngle
		{
			get => _rotationAngle;
			set => SetProperty(ref _rotationAngle, value);
		}

		public Vector2 Offset
		{
			get => _offset;
			set => SetProperty(ref _offset, value);
		}

		public Vector2 CenterPoint
		{
			get => _centerPoint;
			set => SetProperty(ref _centerPoint, value);
		}

		internal Matrix3x2 RelativeTransformMatrix
		{
			get => _relativeTransformMatrix;
			set => SetProperty(ref _relativeTransformMatrix, value);
		}

		internal void InvalidateColorStops()
		{
			OnPropertyChanged(nameof(ColorStops), true);
		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			switch (propertyName)
			{
				case nameof(ColorStops):
					OnColorStopsChanged(ColorStops);
					break;
				case nameof(ExtendMode):
					OnExtendModeChanged(ExtendMode);
					break;
				case nameof(MappingMode):
					OnMappingModeChanged(MappingMode);
					break;
				default:
					break;
			}
		}

		partial void OnExtendModeChanged(CompositionGradientExtendMode extendMode);
		partial void OnColorStopsChanged(CompositionColorGradientStopCollection colorStops);
		partial void OnMappingModeChanged(CompositionMappingMode mappingMode);

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
