#nullable enable

using System;
using System.Globalization;
using System.Numerics;

namespace Microsoft.UI.Composition;

internal sealed class Matrix4x4CreateFromAxisAngleVector3FloatFunctionSpecification : IAnimationFunctionSpecification
{
	private Matrix4x4CreateFromAxisAngleVector3FloatFunctionSpecification()
	{
	}

	public static Matrix4x4CreateFromAxisAngleVector3FloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 2;

	public string MethodName => "CreateFromAxisAngle";

	public string? ClassName => "Matrix4x4";

	// The angle is expressed in radians, matching Composition's CreateFromAxisAngle(Vector3 axis, Float angle).
	public object Evaluate(params object[] parameters)
		=> Matrix4x4.CreateFromAxisAngle(
			(Vector3)parameters[0],
			Convert.ToSingle(parameters[1], CultureInfo.InvariantCulture));
}
