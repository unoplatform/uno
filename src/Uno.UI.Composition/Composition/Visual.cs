#nullable enable

using System.Numerics;

namespace Microsoft.UI.Composition
{
	public partial class Visual : CompositionObject
	{
		private Vector2 _size;
		private Vector3 _offset;
		private Vector3 _scale = new Vector3(1, 1, 1);
		private Vector3 _centerPoint;
		private float _rotationAngleInDegrees;
		private Vector3 _rotationAxis = new Vector3(0, 0, 1);
		private Matrix4x4 _transformMatrix = Matrix4x4.Identity;
		private bool _isVisible = true;
		private float _opacity = 1.0f;
		private CompositionCompositeMode _compositeMode;

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
			set => SetProperty(ref _compositeMode, value);
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

		public float RotationAngleInDegrees
		{
			get => _rotationAngleInDegrees;
			set { SetProperty(ref _rotationAngleInDegrees, value); OnRotationAngleInDegreesChanged(value); }
		}

		partial void OnRotationAngleInDegreesChanged(float value);

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

		internal bool HasThemeShadow { get; set; }

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			// TODO: Determine whether to invalidate renderer based on the fact whether we are attached to a CompositionTarget.
			//bool isAttached = false;
			//if (isAttached)
			//{
			//	Compositor.InvalidateRender();
			//}

			Compositor.InvalidateRender();
		}
	}
}
