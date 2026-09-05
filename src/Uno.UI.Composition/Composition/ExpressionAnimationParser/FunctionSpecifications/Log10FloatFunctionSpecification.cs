#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class Log10FloatFunctionSpecification : IAnimationFunctionSpecification
{
	private Log10FloatFunctionSpecification()
	{
	}

	public static Log10FloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Log10";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Log10(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
