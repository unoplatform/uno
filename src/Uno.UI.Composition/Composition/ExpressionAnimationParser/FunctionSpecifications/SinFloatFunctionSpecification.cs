#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class SinFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private SinFloatFunctionSpecification()
	{
	}

	public static SinFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Sin";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Sin(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
