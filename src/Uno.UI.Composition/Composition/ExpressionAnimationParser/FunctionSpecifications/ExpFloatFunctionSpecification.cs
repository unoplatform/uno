#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class ExpFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private ExpFloatFunctionSpecification()
	{
	}

	public static ExpFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Exp";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Exp(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
