#nullable enable

using System.Numerics;
using Uno.Extensions;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class Visual : CompositionObject, I3DTransformableObject
	{
		private Vector2 _size;
		private Vector3 _offset;
		private Vector3 _scale = new Vector3(1, 1, 1);
		private Vector3 _centerPoint;
		private Quaternion _orientation = Quaternion.Identity;
		private float _rotationAngle;
		private Vector3 _rotationAxis = new Vector3(0, 0, 1);
		private Matrix4x4 _transformMatrix = Matrix4x4.Identity;
		private bool _isVisible = true;
		private float _opacity = 1.0f;
		private CompositionCompositeMode _compositeMode;
		private object? _compositionTarget; // this should be a Microsoft.UI.Xaml.Media.CompositionTarget

		internal Visual(Compositor compositor) : base(compositor)
		{
			InitializePartial();
		}

		partial void InitializePartial();

		public Matrix4x4 TransformMatrix
		{
			get => _transformMatrix;
			set => SetProperty(ref _transformMatrix, value);
		}

		public Vector3 Offset
		{
			get => _offset;
			set { SetProperty(ref _offset, value); OnOffsetChanged(value); }
		}

		partial void OnOffsetChanged(Vector3 value);

		public bool IsVisible
		{
			get => _isVisible;
			set => SetProperty(ref _isVisible, value);
		}

		public CompositionCompositeMode CompositeMode
		{
			get => _compositeMode;
			set => SetEnumProperty(ref _compositeMode, value);
		}

		public Vector3 CenterPoint
		{
			get => _centerPoint;
			set { SetProperty(ref _centerPoint, value); OnCenterPointChanged(value); }
		}

		partial void OnCenterPointChanged(Vector3 value);

		public Vector3 Scale
		{
			get => _scale;
			set { SetProperty(ref _scale, value); OnScaleChanged(value); }
		}

		partial void OnScaleChanged(Vector3 value);

		public Quaternion Orientation
		{
			get => _orientation;
			set { SetProperty(ref _orientation, value); OnOrientationChanged(value); }
		}

		partial void OnOrientationChanged(Quaternion value);

		public float RotationAngleInDegrees
		{
			get => (float)MathEx.ToDegree(_rotationAngle);
			set => RotationAngle = (float)MathEx.ToRadians(value);
		}

		public float RotationAngle
		{
			get => _rotationAngle;
			set { SetProperty(ref _rotationAngle, value); OnRotationAngleChanged(value); }
		}

		partial void OnRotationAngleChanged(float value);

		public Vector2 Size
		{
			get => _size;
			set { SetProperty(ref _size, value); OnSizeChanged(value); }
		}

		partial void OnSizeChanged(Vector2 value);

		public float Opacity
		{
			get => _opacity;
			set => SetProperty(ref _opacity, value);
		}

		public Vector3 RotationAxis
		{
			get => _rotationAxis;
			set { SetProperty(ref _rotationAxis, value); OnRotationAxisChanged(value); }
		}

		partial void OnRotationAxisChanged(Vector3 value);

		public ContainerVisual? Parent { get; set; }

		internal object? CompositionTarget
		{
			get => _compositionTarget ?? Parent?.CompositionTarget; // TODO: can this be cached?
			set => _compositionTarget = value;
		}

		internal bool HasThemeShadow { get; set; }

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			Compositor.InvalidateRender(this);
		}
	}
}
