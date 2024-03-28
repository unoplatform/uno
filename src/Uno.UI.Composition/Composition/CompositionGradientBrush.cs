#nullable enable

using System;
using System.Numerics;
using Uno.Extensions;
using Uno.UI.Composition;

namespace Windows.UI.Composition
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
	}
}
