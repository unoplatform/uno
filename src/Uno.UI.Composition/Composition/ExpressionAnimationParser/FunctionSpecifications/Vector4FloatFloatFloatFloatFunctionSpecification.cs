#nullable enable

using System;
using System.Globalization;
using System.Numerics;

namespace Microsoft.UI.Composition;

internal sealed class Vector4FloatFloatFloatFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private Vector4FloatFloatFloatFloatFunctionSpecification()
	{
	}

	public static Vector4FloatFloatFloatFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 4;

	public string MethodName => "Vector4";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> new Vector4(
			Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[1], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[2], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[3], CultureInfo.InvariantCulture));
}
