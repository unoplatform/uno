#nullable enable

using System;
using System.Globalization;

namespace Windows.UI.Composition;

internal sealed class MaxFloatFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private MaxFloatFloatFunctionSpecification()
	{
	}

	public static MaxFloatFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 2;

	public string MethodName => "Max";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> Math.Max(
			Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[1], CultureInfo.InvariantCulture));
}
