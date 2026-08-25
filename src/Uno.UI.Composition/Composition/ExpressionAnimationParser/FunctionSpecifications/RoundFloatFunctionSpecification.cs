#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class RoundFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private RoundFloatFunctionSpecification()
	{
	}

	public static RoundFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Round";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Round(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
