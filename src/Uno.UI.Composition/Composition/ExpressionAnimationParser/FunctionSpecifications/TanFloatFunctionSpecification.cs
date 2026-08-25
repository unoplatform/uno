#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class TanFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private TanFloatFunctionSpecification()
	{
	}

	public static TanFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Tan";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Tan(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
