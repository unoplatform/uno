#nullable enable
using System;
using System.Linq;
using System.Numerics;
using Uno.UI.Composition;

namespace Uno.Extensions;

internal static class TransformableObjectExtensions
{
	/// <summary>
	/// Gets the total transformation matrix applied on the object.
	/// The resulting matrix only contains transformation related properties, it does **not** include the Offset.
	/// </summary>
	public static Matrix3x2 GetTransform(this I2DTransformableObject transformableObject)
	{
		var transform = transformableObject.TransformMatrix;

		if (transformableObject.Scale != Vector2.One)
		{
			transform *= Matrix3x2.CreateScale(transformableObject.Scale, transformableObject.CenterPoint);
		}

		if (transformableObject.RotationAngle is not 0)
		{
			transform *= Matrix3x2.CreateRotation(transformableObject.RotationAngle, transformableObject.CenterPoint);
		}

		return transform;
	}

	/// <summary>
	/// Gets the total transformation matrix applied on the object.
	/// The resulting matrix only contains transformation related properties, it does **not** include the Offset.
	/// </summary>
	public static Matrix4x4 GetTransform(this I3DTransformableObject transformableObject)
	{
		var transform = transformableObject.TransformMatrix;

		var scale = transformableObject.Scale;
		if (scale != Vector3.One)
		{
			transform *= Matrix4x4.CreateScale(scale, transformableObject.CenterPoint);
		}

		var orientation = transformableObject.Orientation;
		if (orientation != Quaternion.Identity)
		{
			transform *= Matrix4x4.CreateFromQuaternion(orientation);
		}

		var rotation = transformableObject.RotationAngle;
		if (rotation is not 0)
		{
			transform *= Matrix4x4.CreateTranslation(-transformableObject.CenterPoint);
			transform *= Matrix4x4.CreateFromAxisAngle(transformableObject.RotationAxis, rotation);
			transform *= Matrix4x4.CreateTranslation(transformableObject.CenterPoint);
		}

		return transform;
	}
}
