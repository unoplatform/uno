#nullable enable

using System;
using System.Globalization;
using System.Numerics;

namespace Microsoft.UI.Composition;

// Matrix3x2(m11, m12, m21, m22, m31, m32) constructor form emitted by LottieGen.
internal sealed class Matrix3x2FunctionSpecification : IAnimationFunctionSpecification
{
	private Matrix3x2FunctionSpecification()
	{
	}

	public static Matrix3x2FunctionSpecification Instance { get; } = new();

	public int ParametersLength => 6;

	public string MethodName => "Matrix3x2";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> new Matrix3x2(
			Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[1], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[2], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[3], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[4], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[5], CultureInfo.InvariantCulture));
}
