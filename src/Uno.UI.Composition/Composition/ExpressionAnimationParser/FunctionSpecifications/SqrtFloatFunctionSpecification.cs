#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class SqrtFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private SqrtFloatFunctionSpecification()
	{
	}

	public static SqrtFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Sqrt";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Sqrt(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
