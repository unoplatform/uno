#nullable enable

using System;
using System.Numerics;
using Windows.UI.Core;

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
		private Matrix4x4 transformMatrix = Matrix4x4.Identity;
		private bool isVisible = true;
		private float opacity = 1.0f;

		internal Visual(Compositor compositor) : base(compositor)
		{
			InitializePartial();
		}

		partial void InitializePartial();

		public Matrix4x4 TransformMatrix
		{
			get => transformMatrix;
			set
			{
				transformMatrix = value;
				Compositor.InvalidateRender();
			}
		}
		public Vector3 Offset
		{
			get => _offset;
			set
			{
				_offset = value;
				OnOffsetChanged(value);
				Compositor.InvalidateRender();
			}
		}

		partial void OnOffsetChanged(Vector3 value);

		public bool IsVisible
		{
			get => isVisible;
			set
			{
				isVisible = value;
				Compositor.InvalidateRender();
			}
		}

		public CompositionCompositeMode CompositeMode { get; set; }

		public Vector3 CenterPoint
		{
			get => _centerPoint;
			set
			{
				_centerPoint = value; OnCenterPointChanged(value);
				Compositor.InvalidateRender();
			}
		}

		partial void OnCenterPointChanged(Vector3 value);

		public global::System.Numerics.Vector3 Scale
		{
			get => _scale;
			set
			{
				_scale = value;
				OnScaleChanged(value);
				Compositor.InvalidateRender();
			}
		}

		partial void OnScaleChanged(Vector3 value);

		public float RotationAngleInDegrees
		{
			get => _rotationAngleInDegrees;
			set
			{
				_rotationAngleInDegrees = value; OnRotationAngleInDegreesChanged(value);
				Compositor.InvalidateRender();
			}
		}

		partial void OnRotationAngleInDegreesChanged(float value);

		public Vector2 Size
		{
			get => _size;
			set
			{
				_size = value; OnSizeChanged(value);
				Compositor.InvalidateRender();
			}
		}

		partial void OnSizeChanged(Vector2 value);

		public float Opacity
		{
			get => opacity; set
			{
				opacity = value;
				Compositor.InvalidateRender();
			}
		}
		public Vector3 RotationAxis
		{
			get => _rotationAxis;
			set
			{
				_rotationAxis = value; OnRotationAxisChanged(value);
				Compositor.InvalidateRender();
			}
		}

		partial void OnRotationAxisChanged(Vector3 value);

		public ContainerVisual? Parent { get; set; }
	}
}
