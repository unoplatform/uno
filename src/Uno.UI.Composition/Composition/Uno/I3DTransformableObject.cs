#nullable enable
using System;
using System.Linq;
using System.Numerics;

namespace Uno.UI.Composition;

/// <summary>
/// A composition object that can be transformed in 3D.
/// </summary>
internal interface I3DTransformableObject
{
	Matrix4x4 TransformMatrix { get; }
	Vector3 CenterPoint { get; }
	Vector3 Scale { get; }
	Quaternion Orientation { get; }
	float RotationAngle { get; }
	Vector3 RotationAxis { get; }
}
