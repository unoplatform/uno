#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class CosFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private CosFloatFunctionSpecification()
	{
	}

	public static CosFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Cos";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Cos(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
