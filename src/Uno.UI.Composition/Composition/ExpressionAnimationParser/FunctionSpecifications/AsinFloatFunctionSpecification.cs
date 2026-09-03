#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class AsinFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private AsinFloatFunctionSpecification()
	{
	}

	public static AsinFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Asin";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Asin(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
