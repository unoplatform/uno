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

		private Fields _fields = Fields.Create(); // WARNING: Visual can also be modified from a background thread.
		private Fields _renderFields = Fields.Create();
		private ContainerVisual? _parent;

		internal Visual(Compositor compositor) : base(compositor)
		{
			InitializePartial();
		}

		partial void InitializePartial();

		public ContainerVisual? Parent
		{
			get => _parent;
			internal set
			{
				if (_parent == value)
				{
					return;
				}

				_parent = value;
				if (value is { })
				{
					ShareDirtyStateWith(value);
				}
			}
		}

		// TODO: Need to register / un-register from force redraw?
		internal VisualKind Kind { get; set; } = VisualKind.UnknownNativeView; // TODO: By default should be managed

		#region Visual properties
		public Matrix4x4 TransformMatrix
		{
			get => _fields._transformMatrix;
			set
			{
				if (_fields._transformMatrix == value)
				{
					return;
				}

				_fields._transformMatrix = value;

				Invalidate(CompositionPropertyType.Independent);
			}
		}
		public Vector3 Offset
		{
			get => _fields._offset;
			set
			{
				if (_fields._offset == value)
				{
					return;
				}

				_fields._offset = value;
				OnOffsetChanged(value);

				Invalidate(CompositionPropertyType.Independent);
			}
		}

		partial void OnOffsetChanged(Vector3 value);

		public bool IsVisible
		{
			get => _fields._isVisible;
			set
			{
				if (_fields._isVisible == value)
				{
					return;
				}

				_fields._isVisible = value;
				OnIsVisibleChanged(value);

				Invalidate(CompositionPropertyType.Independent);
			}
		}

		partial void OnIsVisibleChanged(bool value);

		public CompositionCompositeMode CompositeMode { get; set; }

		public Vector3 CenterPoint
		{
			get => _fields._centerPoint;
			set
			{
				if (_fields._centerPoint == value)
				{
					return;
				}

				_fields._centerPoint = value;
				OnCenterPointChanged(value);

				Invalidate(CompositionPropertyType.Independent);
			}
		}

		partial void OnCenterPointChanged(Vector3 value);

		public global::System.Numerics.Vector3 Scale
		{
			get => _fields._scale;
			set
			{
				if (_fields._scale == value)
				{
					return;
				}

				_fields._scale = value;
				OnScaleChanged(value);

				Invalidate(CompositionPropertyType.Independent);
			}
		}

		partial void OnScaleChanged(Vector3 value);

		public float RotationAngleInDegrees
		{
			get => _fields._rotationAngleInDegrees;
			set
			{
				if (_fields._rotationAngleInDegrees == value)
				{
					return;
				}

				_fields._rotationAngleInDegrees = value;
				OnRotationAngleInDegreesChanged(value);

				Invalidate(CompositionPropertyType.Independent);
			}
		}

		partial void OnRotationAngleInDegreesChanged(float value);

		public Vector2 Size
		{
			get => _fields._size;
			set
			{
				if (_fields._size == value)
				{
					return;
				}

				_fields._size = value;
				OnSizeChanged(value);

				Invalidate(CompositionPropertyType.Independent);
			}
		}

		partial void OnSizeChanged(Vector2 value);

		public float Opacity
		{
			get => _fields._opacity;
			set
			{
				if (_fields._opacity == value)
				{
					return;
				}

				_fields._opacity = value;
				OnOpacityChanged(value);

				Invalidate(CompositionPropertyType.Independent);
			}
		}
		partial void OnOpacityChanged(float value);

		public Vector3 RotationAxis
		{
			get => _fields._rotationAxis;
			set
			{
				if (_fields._rotationAxis == value)
				{
					return;
				}

				_fields._rotationAxis = value;
				OnRotationAxisChanged(value);

				Invalidate(CompositionPropertyType.Independent);
			}
		}

		partial void OnRotationAxisChanged(Vector3 value);

		public CompositionClip? Clip
		{
			get => _fields._clip;
			set => _fields._clip = value;
		}
		#endregion
	}
}
