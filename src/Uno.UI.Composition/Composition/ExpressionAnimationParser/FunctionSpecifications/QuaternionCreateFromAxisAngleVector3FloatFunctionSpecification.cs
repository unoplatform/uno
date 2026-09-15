#nullable enable

using System;
using System.Globalization;
using System.Numerics;

namespace Microsoft.UI.Composition;

internal sealed class QuaternionCreateFromAxisAngleVector3FloatFunctionSpecification : IAnimationFunctionSpecification
{
	private QuaternionCreateFromAxisAngleVector3FloatFunctionSpecification()
	{
	}

	public static QuaternionCreateFromAxisAngleVector3FloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 2;

	public string MethodName => "CreateFromAxisAngle";

	public string? ClassName => "Quaternion";

	// The angle is expressed in radians, matching Composition's CreateFromAxisAngle(Vector3 axis, Scalar angle).
	public object Evaluate(params object[] parameters)
		=> Quaternion.CreateFromAxisAngle(
			(Vector3)parameters[0],
			Convert.ToSingle(parameters[1], CultureInfo.InvariantCulture));
}
