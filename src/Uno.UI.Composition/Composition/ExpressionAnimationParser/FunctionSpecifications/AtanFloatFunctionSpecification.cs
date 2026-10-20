#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class AtanFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private AtanFloatFunctionSpecification()
	{
	}

	public static AtanFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Atan";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Atan(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
