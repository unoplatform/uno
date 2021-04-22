#nullable enable

using System;
using System.Numerics;
using Windows.UI.Core;

namespace Windows.UI.Composition
{
	public partial class Visual : global::Windows.UI.Composition.CompositionObject
	{
		private struct Fields
		{
			public static Fields Create()
				=> new Fields
				{
					_scale = new Vector3(1, 1, 1),
					_rotationAxis = new Vector3(0, 0, 1),
					_transformMatrix = Matrix4x4.Identity,
					_isVisible = true,
					_opacity = 1.0f
				};

			public Vector2 _size;
			public Vector3 _offset;
			public Vector3 _scale;
			public Vector3 _centerPoint;
			public float _rotationAngleInDegrees;
			public Vector3 _rotationAxis;
			public Matrix4x4 _transformMatrix;
			public bool _isVisible;
			public float _opacity;
			public CompositionClip? _clip;
		}

		private Fields _ui = Fields.Create(); // WARNING: Visual can also be modified from a background thread. This is 
		private Fields _render = Fields.Create();

		internal Visual(Compositor compositor) : base(compositor)
		{
			InitializePartial();
		}

		partial void InitializePartial();

		public ContainerVisual? Parent { get; internal set; }

		// TODO: Need to register / un-register from force redraw?
		internal VisualKind Kind { get; set; } = VisualKind.UnknownNativeView; // TODO: By default should be managed

		#region Visual properties
		public Matrix4x4 TransformMatrix
		{
			get => _ui._transformMatrix;
			set
			{
				if (_ui._transformMatrix == value)
				{
					return;
				}

				_ui._transformMatrix = value;

				Invalidate(VisualDirtyState.Independent);
			}
		}
		public Vector3 Offset
		{
			get => _ui._offset;
			set
			{
				if (_ui._offset == value)
				{
					return;
				}

				_ui._offset = value;
				OnOffsetChanged(value);

				Invalidate(VisualDirtyState.Independent);
			}
		}

		partial void OnOffsetChanged(Vector3 value);

		public bool IsVisible
		{
			get => _ui._isVisible;
			set
			{
				if (_ui._isVisible == value)
				{
					return;
				}

				_ui._isVisible = value;
				OnIsVisibleChanged(value);

				Invalidate(VisualDirtyState.Independent);
			}
		}

		partial void OnIsVisibleChanged(bool value);

		public CompositionCompositeMode CompositeMode { get; set; }

		public Vector3 CenterPoint
		{
			get => _ui._centerPoint;
			set
			{
				if (_ui._centerPoint == value)
				{
					return;
				}

				_ui._centerPoint = value;
				OnCenterPointChanged(value);

				Invalidate(VisualDirtyState.Independent);
			}
		}

		partial void OnCenterPointChanged(Vector3 value);

		public global::System.Numerics.Vector3 Scale
		{
			get => _ui._scale;
			set
			{
				if (_ui._scale == value)
				{
					return;
				}

				_ui._scale = value;
				OnScaleChanged(value);

				Invalidate(VisualDirtyState.Independent);
			}
		}

		partial void OnScaleChanged(Vector3 value);

		public float RotationAngleInDegrees
		{
			get => _ui._rotationAngleInDegrees;
			set
			{
				if (_ui._rotationAngleInDegrees == value)
				{
					return;
				}

				_ui._rotationAngleInDegrees = value;
				OnRotationAngleInDegreesChanged(value);

				Invalidate(VisualDirtyState.Independent);
			}
		}

		partial void OnRotationAngleInDegreesChanged(float value);

		public Vector2 Size
		{
			get => _ui._size;
			set
			{
				if (_ui._size == value)
				{
					return;
				}

				_ui._size = value;
				OnSizeChanged(value);

				Invalidate(VisualDirtyState.Independent);
			}
		}

		partial void OnSizeChanged(Vector2 value);

		public float Opacity
		{
			get => _ui._opacity;
			set
			{
				if (_ui._opacity == value)
				{
					return;
				}

				_ui._opacity = value;
				OnOpacityChanged(value);

				Invalidate(VisualDirtyState.Independent);
			}
		}
		partial void OnOpacityChanged(float value);

		public Vector3 RotationAxis
		{
			get => _ui._rotationAxis;
			set
			{
				if (_ui._rotationAxis == value)
				{
					return;
				}

				_ui._rotationAxis = value;
				OnRotationAxisChanged(value);

				Invalidate(VisualDirtyState.Independent);
			}
		}

		partial void OnRotationAxisChanged(Vector3 value);

		public CompositionClip? Clip
		{
			get => _ui._clip;
			set => _ui._clip = value;
		}
		#endregion
	}
}
