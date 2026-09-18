#nullable enable

using System;
using System.Globalization;
using System.Numerics;

namespace Microsoft.UI.Composition;

internal sealed class Matrix3x2CreateRotationFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private Matrix3x2CreateRotationFloatFunctionSpecification()
	{
	}

	public static Matrix3x2CreateRotationFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "CreateRotation";

	public string? ClassName => "Matrix3x2";

	// Composition and System.Numerics both express the rotation angle in radians.
	public object Evaluate(params object[] parameters)
		=> Matrix3x2.CreateRotation(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
