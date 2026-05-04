#nullable enable

using System;
using System.Numerics;
using Uno.Extensions;
using Uno.UI.Composition;

using static Microsoft.UI.Composition.SubPropertyHelpers;

namespace Microsoft.UI.Composition;

public partial class CompositionShape : CompositionObject, I2DTransformableObject
{
	private Matrix3x2 _transformMatrix = Matrix3x2.Identity;
	private Vector2 _scale = new Vector2(1, 1);
	private float _rotationAngle;
	private Vector2 _offset;
	private Vector2 _centerPoint;

	internal CompositionShape() => throw new InvalidOperationException($"Use the Compositor ctor");

	internal CompositionShape(Compositor compositor) : base(compositor)
	{
	}

	public Matrix3x2 TransformMatrix
	{
		get => _transformMatrix;
		set => SetProperty(ref _transformMatrix, value);
	}

	public Vector2 Scale
	{
		get => _scale;
		set => SetProperty(ref _scale, value);
	}

	public float RotationAngleInDegrees
	{
		get => (float)MathEx.ToDegree(_rotationAngle);
		set => RotationAngle = (float)MathEx.ToRadians(value);
	}

	public float RotationAngle
	{
		get => _rotationAngle;
		set => SetProperty(ref _rotationAngle, value);
	}

	public Vector2 Offset
	{
		get => _offset;
		set => SetProperty(ref _offset, value);
	}

	public Vector2 CenterPoint
	{
		get => _centerPoint;
		set => SetProperty(ref _centerPoint, value);
	}

	internal override object GetAnimatableProperty(string propertyName, string subPropertyName)
	{
		if (propertyName.Equals(nameof(Offset), StringComparison.OrdinalIgnoreCase))
		{
			return GetVector2(subPropertyName, Offset);
		}
		else if (propertyName.Equals(nameof(Scale), StringComparison.OrdinalIgnoreCase))
		{
			return GetVector2(subPropertyName, Scale);
		}
		else if (propertyName.Equals(nameof(CenterPoint), StringComparison.OrdinalIgnoreCase))
		{
			return GetVector2(subPropertyName, CenterPoint);
		}
		else if (propertyName.Equals(nameof(RotationAngle), StringComparison.OrdinalIgnoreCase))
		{
			return RotationAngle;
		}
		else if (propertyName.Equals(nameof(TransformMatrix), StringComparison.OrdinalIgnoreCase))
		{
			return GetMatrix3x2(subPropertyName, TransformMatrix);
		}
		else
		{
			return base.GetAnimatableProperty(propertyName, subPropertyName);
		}
	}

	private protected override void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
	{
		if (propertyName.Equals(nameof(Offset), StringComparison.OrdinalIgnoreCase))
		{
			Offset = UpdateVector2(subPropertyName, Offset, propertyValue);
		}
		else if (propertyName.Equals(nameof(Scale), StringComparison.OrdinalIgnoreCase))
		{
			Scale = UpdateVector2(subPropertyName, Scale, propertyValue);
		}
		else if (propertyName.Equals(nameof(CenterPoint), StringComparison.OrdinalIgnoreCase))
		{
			CenterPoint = UpdateVector2(subPropertyName, CenterPoint, propertyValue);
		}
		else if (propertyName.Equals(nameof(RotationAngle), StringComparison.OrdinalIgnoreCase))
		{
			RotationAngle = ValidateValue<float>(propertyValue);
		}
		else if (propertyName.Equals(nameof(TransformMatrix), StringComparison.OrdinalIgnoreCase))
		{
			TransformMatrix = UpdateMatrix3x2(subPropertyName, TransformMatrix, propertyValue);
		}
		else
		{
			base.SetAnimatableProperty(propertyName, subPropertyName, propertyValue);
		}
	}
}
