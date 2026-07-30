#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class PowFloatFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private PowFloatFloatFunctionSpecification()
	{
	}

	public static PowFloatFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 2;

	public string MethodName => "Pow";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Pow(
			Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture),
			Convert.ToSingle(parameters[1], CultureInfo.InvariantCulture));
}
