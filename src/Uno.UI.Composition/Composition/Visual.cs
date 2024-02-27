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
			if (propertyName is
				nameof(AnchorPoint) or
				nameof(CenterPoint) or
				nameof(Offset) or
				nameof(Opacity) or
				nameof(Orientation) or
				nameof(RotationAngle) or
				nameof(RotationAxis) or
				nameof(Size) or
				nameof(TransformMatrix))
			{
				return true;
			}

			return base.IsAnimatableProperty(propertyName);
		}

		private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (propertyName is nameof(AnchorPoint))
			{
				AnchorPoint = UpdateVector2(subPropertyName, AnchorPoint, propertyValue);
			}
			else if (propertyName is nameof(CenterPoint))
			{
				CenterPoint = UpdateVector3(subPropertyName, CenterPoint, propertyValue);
			}
			else if (propertyName is nameof(Offset))
			{
				Offset = UpdateVector3(subPropertyName, Offset, propertyValue);
			}
			else if (propertyName is nameof(Opacity))
			{
				Opacity = ValidateValue<float>(propertyValue);
			}
			else if (propertyName is nameof(Orientation))
			{
				Orientation = UpdateQuaternion(subPropertyName, Orientation, propertyValue);
			}
			else if (propertyName is nameof(RotationAngle))
			{
				RotationAngle = ValidateValue<float>(propertyValue);
			}
			else if (propertyName is nameof(RotationAxis))
			{
				RotationAxis = UpdateVector3(subPropertyName, RotationAxis, propertyValue);
			}
			else if (propertyName is nameof(Size))
			{
				Size = UpdateVector2(subPropertyName, Size, propertyValue);
			}
			else if (propertyName is nameof(TransformMatrix))
			{
				TransformMatrix = UpdateMatrix4x4(subPropertyName, TransformMatrix, propertyValue);
			}
			else
			{
				base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
			}
		}
	}
}
