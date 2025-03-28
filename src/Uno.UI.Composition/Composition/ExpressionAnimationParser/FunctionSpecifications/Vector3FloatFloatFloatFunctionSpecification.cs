#nullable enable

using System;
using System.Globalization;
using System.Numerics;

namespace Windows.UI.Composition;

internal sealed class Vector3FloatFloatFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private Vector3FloatFloatFloatFunctionSpecification()
	{
	}

	public static Vector3FloatFloatFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 3;

	public string MethodName => "Vector3";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> new Vector3(
			Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[1], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[2], CultureInfo.InvariantCulture));
}
