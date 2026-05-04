#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class SquareFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private SquareFloatFunctionSpecification()
	{
	}

	public static SquareFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Square";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
	{
		var x = Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture);
		return x * x;
	}
}
