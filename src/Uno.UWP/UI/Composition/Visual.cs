using System;
using System.Numerics;

namespace Windows.UI.Composition
{
	public partial class Visual : global::Windows.UI.Composition.CompositionObject
	{
		private Vector2 _size;
		private Vector3 _offset;
		private Vector3 _scale = new Vector3(1, 1, 1);
		private Vector3 _centerPoint;
		private float _rotationAngleInDegrees;
		private Vector3 _rotationAxis = new Vector3(0, 0, 1);

		public Visual(Compositor compositor) : base(compositor)
		{

		}

		public Matrix4x4 TransformMatrix { get; set; } = Matrix4x4.Identity;

		public Vector3 Offset
		{
			get { return _offset; }
			set { _offset = value; OnOffsetChanged(value); }
		}

		partial void OnOffsetChanged(Vector3 value);

		public bool IsVisible { get; set; }

		public CompositionCompositeMode CompositeMode { get; set; }

		public Vector3 CenterPoint
		{
			get { return _centerPoint; }
			set { _centerPoint = value; OnCenterPointChanged(value); }
		}

		partial void OnCenterPointChanged(Vector3 value);

		public global::System.Numerics.Vector3 Scale
		{
			get { return _scale; }
			set { _scale = value; OnScaleChanged(value); }
		}

		partial void OnScaleChanged(Vector3 value);

		public float RotationAngleInDegrees
		{
			get { return _rotationAngleInDegrees; }
			set { _rotationAngleInDegrees = value; OnRotationAngleInDegreesChanged(value); }
		}

		partial void OnRotationAngleInDegreesChanged(float value);

		public Vector2 Size
		{
			get { return _size; }
			set { _size = value; OnSizeChanged(value); }
		}

		partial void OnSizeChanged(Vector2 value);

		public float Opacity { get; set; } = 1.0f;

		public Vector3 RotationAxis
		{
			get { return _rotationAxis; }
			set { _rotationAxis = value; OnRotationAxisChanged(value); }
		}

		partial void OnRotationAxisChanged(Vector3 value);

		public ContainerVisual Parent { get; set; }
	}
}
