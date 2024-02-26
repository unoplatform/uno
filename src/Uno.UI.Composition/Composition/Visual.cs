#nullable enable

using System;
using System.Numerics;
using Microsoft.UI.Composition.Interactions;
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
		private ICompositionTarget? _compositionTarget;

		internal Visual(Compositor compositor) : base(compositor)
		{
			InitializePartial();
		}

		partial void InitializePartial();

		internal VisualInteractionSource? VisualInteractionSource { get; set; }

		internal bool IsTranslationEnabled { get; set; }

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

		internal ICompositionTarget? CompositionTarget
		{
			get => _compositionTarget ?? Parent?.CompositionTarget; // TODO: can this be cached?
			set => _compositionTarget = value;
		}

		internal bool HasThemeShadow { get; set; }

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			Compositor.InvalidateRender(this);
		}

		private protected override bool IsAnimatableProperty(ReadOnlySpan<char> propertyName)
		{
			return propertyName is
				nameof(AnchorPoint) or
				nameof(CenterPoint) or
				nameof(Offset) or
				nameof(Opacity) or
				nameof(Orientation) or
				nameof(RotationAngle) or
				nameof(RotationAxis) or
				nameof(Size) or
				nameof(TransformMatrix);
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName is nameof(AnchorPoint) && subPropertyName.Length == 0)
			{
				AnchorPoint = ValidateValue<Vector2>(propertyValue);
			}
			else if (propertyName is nameof(AnchorPoint) && subPropertyName.Equals("X", StringComparison.Ordinal))
			{
				var x = ValidateValue<float>(propertyValue);
				AnchorPoint = new Vector2(x, AnchorPoint.Y);
			}
			else if (propertyName is nameof(AnchorPoint) && subPropertyName.Equals("Y", StringComparison.Ordinal))
			{
				var y = ValidateValue<float>(propertyValue);
				AnchorPoint = new Vector2(AnchorPoint.X, y);
			}
			else if (propertyName is nameof(CenterPoint) && subPropertyName.Length == 0)
			{
				CenterPoint = ValidateValue<Vector3>(propertyValue);
			}
			else if (propertyName is nameof(CenterPoint) && subPropertyName.Equals("X", StringComparison.Ordinal))
			{
				var x = ValidateValue<float>(propertyValue);
				CenterPoint = new Vector3(x, CenterPoint.Y, CenterPoint.Z);
			}
			else if (propertyName is nameof(CenterPoint) && subPropertyName.Equals("Y", StringComparison.Ordinal))
			{
				var y = ValidateValue<float>(propertyValue);
				CenterPoint = new Vector3(CenterPoint.X, y, CenterPoint.Z);
			}
			else if (propertyName is nameof(CenterPoint) && subPropertyName.Equals("Z", StringComparison.Ordinal))
			{
				var z = ValidateValue<float>(propertyValue);
				CenterPoint = new Vector3(CenterPoint.X, CenterPoint.Y, z);
			}
			else if (propertyName is nameof(Offset) && subPropertyName.Length == 0)
			{
				Offset = ValidateValue<Vector3>(propertyValue);
			}
			else if (propertyName is nameof(Offset) && subPropertyName.Equals("X", StringComparison.Ordinal))
			{
				var x = ValidateValue<float>(propertyValue);
				Offset = new Vector3(x, Offset.Y, Offset.Z);
			}
			else if (propertyName is nameof(Offset) && subPropertyName.Equals("Y", StringComparison.Ordinal))
			{
				var y = ValidateValue<float>(propertyValue);
				Offset = new Vector3(Offset.X, y, Offset.Z);
			}
			else if (propertyName is nameof(Offset) && subPropertyName.Equals("Z", StringComparison.Ordinal))
			{
				var z = ValidateValue<float>(propertyValue);
				Offset = new Vector3(Offset.X, Offset.Y, z);
			}
			else if (propertyName is nameof(Opacity))
			{
				Opacity = ValidateValue<float>(propertyValue);
			}
			else if (propertyName is nameof(Orientation))
			{
				// TODO: Support X, Y, Z, and W
				Orientation = ValidateValue<Quaternion>(propertyValue);
			}
			else if (propertyName is nameof(RotationAngle))
			{
				RotationAngle = ValidateValue<float>(propertyValue);
			}
			else if (propertyName is nameof(RotationAxis))
			{
				// TODO: Support X, Y, and Z
				RotationAxis = ValidateValue<Vector3>(propertyValue);
			}
			else if (propertyName is nameof(Size) && subPropertyName.Length == 0)
			{
				Size = ValidateValue<Vector2>(propertyValue);
			}
			else if (propertyName is nameof(Size) && subPropertyName.Equals("X", StringComparison.Ordinal))
			{
				var x = ValidateValue<float>(propertyValue);
				Size = new Vector2(x, Size.Y);
			}
			else if (propertyName is nameof(Size) && subPropertyName.Equals("Y", StringComparison.Ordinal))
			{
				var y = ValidateValue<float>(propertyValue);
				Size = new Vector2(Size.X, y);
			}
			else if (propertyName is nameof(TransformMatrix))
			{
				// TODO: Support sub properties.
				TransformMatrix = ValidateValue<Matrix4x4>(propertyValue);
			}
			else
			{
				throw new Exception($"Unable to set property '{propertyName}' on {this}");
			}
		}
	}
}
