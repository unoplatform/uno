#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class CeilFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private CeilFloatFunctionSpecification()
	{
	}

	public static CeilFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Ceil";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Ceiling(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
