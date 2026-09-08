#nullable enable

using System;
using System.Globalization;
using System.Numerics;

namespace Microsoft.UI.Composition;

// Matrix4x4(m11 .. m44) constructor form emitted by LottieGen.
internal sealed class Matrix4x4FunctionSpecification : IAnimationFunctionSpecification
{
	private Matrix4x4FunctionSpecification()
	{
	}

	public static Matrix4x4FunctionSpecification Instance { get; } = new();

	public int ParametersLength => 16;

	public string MethodName => "Matrix4x4";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> new Matrix4x4(
			Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[1], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[2], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[3], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[4], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[5], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[6], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[7], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[8], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[9], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[10], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[11], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[12], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[13], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[14], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[15], CultureInfo.InvariantCulture));
}
