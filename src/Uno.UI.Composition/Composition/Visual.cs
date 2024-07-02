#nullable enable

using System;
using System.Numerics;
using Windows.UI.Composition.Interactions;
using Uno.Extensions;
using Uno.UI.Composition;

using static Windows.UI.Composition.SubPropertyHelpers;

namespace Windows.UI.Composition
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
		private ContainerVisual? _parent;

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

		public ContainerVisual? Parent
		{
			get => _parent;
			set
			{
				_parent = value;
#if __SKIA__
				SetAsPopupVisual(value?.IsPopupVisual ?? false, inherited: true);
#endif
			}
		}

		internal ICompositionTarget? CompositionTarget
		{
			get => _compositionTarget ?? Parent?.CompositionTarget; // TODO: can this be cached?
			set => _compositionTarget = value;
		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			Compositor.InvalidateRender(this);
		}

		internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
		{
			if (propertyName.Equals(nameof(AnchorPoint), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector2(subPropertyName, AnchorPoint);
			}
			else if (propertyName.Equals(nameof(CenterPoint), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector3(subPropertyName, CenterPoint);
			}
			else if (propertyName.Equals(nameof(Offset), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector3(subPropertyName, Offset);
			}
			else if (propertyName.Equals(nameof(Opacity), StringComparison.OrdinalIgnoreCase))
			{
				return Opacity;
			}
			else if (propertyName.Equals(nameof(Orientation), StringComparison.OrdinalIgnoreCase))
			{
				return GetQuaternion(subPropertyName, Orientation);
			}
			else if (propertyName.Equals(nameof(RotationAngle), StringComparison.OrdinalIgnoreCase))
			{
				return RotationAngle;
			}
			else if (propertyName.Equals(nameof(RotationAxis), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector3(subPropertyName, RotationAxis);
			}
			else if (propertyName.Equals(nameof(Size), StringComparison.OrdinalIgnoreCase))
			{
				return GetVector2(subPropertyName, Size);
			}
			else if (propertyName.Equals(nameof(TransformMatrix), StringComparison.OrdinalIgnoreCase))
			{
				return GetMatrix4x4(subPropertyName, TransformMatrix);
			}
			else
			{
				return base.GetAnimatableProperty(propertyName, subPropertyName);
			}
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName.Equals(nameof(AnchorPoint), StringComparison.OrdinalIgnoreCase))
			{
				AnchorPoint = UpdateVector2(subPropertyName, AnchorPoint, propertyValue);
			}
			else if (propertyName.Equals(nameof(CenterPoint), StringComparison.OrdinalIgnoreCase))
			{
				CenterPoint = UpdateVector3(subPropertyName, CenterPoint, propertyValue);
			}
			else if (propertyName.Equals(nameof(Offset), StringComparison.OrdinalIgnoreCase))
			{
				Offset = UpdateVector3(subPropertyName, Offset, propertyValue);
			}
			else if (propertyName.Equals(nameof(Opacity), StringComparison.OrdinalIgnoreCase))
			{
				Opacity = ValidateValue<float>(propertyValue);
			}
			else if (propertyName.Equals(nameof(Orientation), StringComparison.OrdinalIgnoreCase))
			{
				Orientation = UpdateQuaternion(subPropertyName, Orientation, propertyValue);
			}
			else if (propertyName.Equals(nameof(RotationAngle), StringComparison.OrdinalIgnoreCase))
			{
				RotationAngle = ValidateValue<float>(propertyValue);
			}
			else if (propertyName.Equals(nameof(RotationAxis), StringComparison.OrdinalIgnoreCase))
			{
				RotationAxis = UpdateVector3(subPropertyName, RotationAxis, propertyValue);
			}
			else if (propertyName.Equals(nameof(Size), StringComparison.OrdinalIgnoreCase))
			{
				Size = UpdateVector2(subPropertyName, Size, propertyValue);
			}
			else if (propertyName.Equals(nameof(TransformMatrix), StringComparison.OrdinalIgnoreCase))
			{
				TransformMatrix = UpdateMatrix4x4(subPropertyName, TransformMatrix, propertyValue);
			}
			else if (propertyName.Equals(nameof(Scale), StringComparison.OrdinalIgnoreCase))
			{
				Scale = UpdateVector3(subPropertyName, Scale, propertyValue);
			}
			else if (IsTranslationEnabled && propertyName.Equals("Translation", StringComparison.OrdinalIgnoreCase))
			{
				_ = Properties.TryGetVector3("Translation", out var translation);
				Properties.InsertVector3("Translation", UpdateVector3(subPropertyName, translation, propertyValue));
				Compositor.InvalidateRender(this);
			}
			else
			{
				base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
			}
		}
	}
}
