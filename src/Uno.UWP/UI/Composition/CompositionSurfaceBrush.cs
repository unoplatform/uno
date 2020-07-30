#nullable enable
using System;
using System.Numerics;

namespace Windows.UI.Composition
{
	public partial class CompositionSurfaceBrush : CompositionBrush
	{
		private Matrix3x2 _transformMatrix = Matrix3x2.Identity;
		private Vector2 _scale;
		private float _rotationAngleInDegrees;
		private float _rotationAngle;
		private Vector2 _offset;
		private Vector2 _centerPoint;
		private Vector2 _anchorPoint;
		private CompositionStretch _stretch;
		private ICompositionSurface? _surface;
		private float _horizontalAlignmentRatio;
		private bool _snapToPixels;
		private float _verticalAlignmentRatio;
		private CompositionBitmapInterpolationMode bitmapInterpolationMode;

		internal event Action? PropertyChanged;

		public CompositionSurfaceBrush(Compositor compositor) : base(compositor)
		{
		}

		public CompositionSurfaceBrush(Compositor compositor, ICompositionSurface surface) : base(compositor)
		{
			Surface = surface;
		}

		public float VerticalAlignmentRatio
		{
			get => _verticalAlignmentRatio; set
			{
				_verticalAlignmentRatio = value;
				PropertyChanged?.Invoke();
			}
		}

		public ICompositionSurface? Surface
		{
			get => _surface; set
			{
				_surface = value;
				PropertyChanged?.Invoke();
			}
		}

		public CompositionStretch Stretch
		{
			get => _stretch; set
			{
				_stretch = value;
				PropertyChanged?.Invoke();
			}
		}

		public float HorizontalAlignmentRatio
		{
			get => _horizontalAlignmentRatio; set
			{
				_horizontalAlignmentRatio = value;
				PropertyChanged?.Invoke();
			}
		}

		public CompositionBitmapInterpolationMode BitmapInterpolationMode
		{
			get => bitmapInterpolationMode; set
			{
				bitmapInterpolationMode = value;
				PropertyChanged?.Invoke();
			}
		}

		public Matrix3x2 TransformMatrix
		{
			get => _transformMatrix; set
			{
				_transformMatrix = value;
				PropertyChanged?.Invoke();
			}
		}

		public Vector2 Scale
		{
			get => _scale; set
			{
				_scale = value;
				PropertyChanged?.Invoke();
			}
		}

		public float RotationAngleInDegrees
		{
			get => _rotationAngleInDegrees; set
			{
				_rotationAngleInDegrees = value;
				PropertyChanged?.Invoke();
			}
		}

		public float RotationAngle
		{
			get => _rotationAngle; set
			{
				_rotationAngle = value;
				PropertyChanged?.Invoke();
			}
		}

		public Vector2 Offset
		{
			get => _offset; set
			{
				_offset = value;
				PropertyChanged?.Invoke();
			}
		}

		public Vector2 CenterPoint
		{
			get => _centerPoint; set
			{
				_centerPoint = value;
				PropertyChanged?.Invoke();
			}
		}

		public Vector2 AnchorPoint
		{
			get => _anchorPoint; set
			{
				_anchorPoint = value;
				PropertyChanged?.Invoke();
			}
		}

		public bool SnapToPixels
		{
			get => _snapToPixels; set
			{
				_snapToPixels = value;
				PropertyChanged?.Invoke();
			}
		}
	}
}
