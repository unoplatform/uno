#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class FloorFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private FloorFloatFunctionSpecification()
	{
	}

	public static FloorFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Floor";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Floor(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
