#nullable enable
using System;
using System.Linq;
using System.Numerics;

namespace Uno.UI.Composition;

/// <summary>
/// A composition object that can be transformed in 2D.
/// </summary>
internal interface I2DTransformableObject
{
	Matrix3x2 TransformMatrix { get; }
	Vector2 Scale { get; }
	float RotationAngle { get; }
	Vector2 CenterPoint { get; }
}
