#nullable enable

using System;
using System.Globalization;
using System.Numerics;

namespace Microsoft.UI.Composition;

internal sealed class Matrix3x2CreateSkewFloatFloatVector2FunctionSpecification : IAnimationFunctionSpecification
{
	private Matrix3x2CreateSkewFloatFloatVector2FunctionSpecification()
	{
	}

	public static Matrix3x2CreateSkewFloatFloatVector2FunctionSpecification Instance { get; } = new();

	public int ParametersLength => 3;

	public string MethodName => "CreateSkew";

	public string? ClassName => "Matrix3x2";

	// Skew angles are expressed in radians, matching Composition's CreateSkew(Float x, Float y, Vector2 centerpoint).
	public object Evaluate(params object[] parameters)
		=> Matrix3x2.CreateSkew(
			Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[1], CultureInfo.InvariantCulture),
			(Vector2)parameters[2]);
}
