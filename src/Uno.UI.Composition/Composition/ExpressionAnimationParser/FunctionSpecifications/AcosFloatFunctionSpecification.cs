#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class AcosFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private AcosFloatFunctionSpecification()
	{
	}

	public static AcosFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Acos";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Acos(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
