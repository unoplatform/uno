#nullable enable

using System;
using System.Globalization;

namespace Windows.UI.Composition;

internal sealed class MinFloatFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private MinFloatFloatFunctionSpecification()
	{
	}

	public static MinFloatFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 2;

	public string MethodName => "Min";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> Math.Min(
			Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[1], CultureInfo.InvariantCulture));
}
