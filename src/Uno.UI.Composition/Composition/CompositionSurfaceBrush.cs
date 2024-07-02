#nullable enable

using System.Numerics;
using Uno.Extensions;
using Uno.UI.Composition;

namespace Windows.UI.Composition
{
	public partial class CompositionSurfaceBrush : CompositionBrush, I2DTransformableObject
	{
		private Matrix3x2 _transformMatrix = Matrix3x2.Identity;
		private Vector2 _scale;
		private float _rotationAngle;
		private Vector2 _offset;
		private Vector2 _centerPoint;
		private Vector2 _anchorPoint;
		private CompositionStretch _stretch;
		private ICompositionSurface? _surface;
		private float _horizontalAlignmentRatio;
		private bool _snapToPixels;
		private float _verticalAlignmentRatio;
		private CompositionBitmapInterpolationMode _bitmapInterpolationMode;

		internal CompositionSurfaceBrush(Compositor compositor) : base(compositor)
		{
		}

		internal CompositionSurfaceBrush(Compositor compositor, ICompositionSurface surface) : base(compositor)
		{
			Surface = surface;
		}

		public float VerticalAlignmentRatio
		{
			get => _verticalAlignmentRatio;
			set => SetProperty(ref _verticalAlignmentRatio, value);
		}

		public ICompositionSurface? Surface
		{
			get => _surface;
			set => SetObjectProperty(ref _surface, value);
		}

		public CompositionStretch Stretch
		{
			get => _stretch;
			set => SetEnumProperty(ref _stretch, value);
		}

		public float HorizontalAlignmentRatio
		{
			get => _horizontalAlignmentRatio;
			set => SetProperty(ref _horizontalAlignmentRatio, value);
		}

		public CompositionBitmapInterpolationMode BitmapInterpolationMode
		{
			get => _bitmapInterpolationMode;
			set => SetEnumProperty(ref _bitmapInterpolationMode, value);
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

		public Vector2 AnchorPoint
		{
			get => _anchorPoint;
			set => SetProperty(ref _anchorPoint, value);
		}

		public bool SnapToPixels
		{
			get => _snapToPixels;
			set => SetProperty(ref _snapToPixels, value);
		}
	}
}
