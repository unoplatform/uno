#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Composition;

internal sealed class LogFloatFunctionSpecification : IAnimationFunctionSpecification
{
	private LogFloatFunctionSpecification()
	{
	}

	public static LogFloatFunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "Log";

	public string? ClassName => null;

	public object Evaluate(params object[] parameters)
		=> MathF.Log(Convert.ToSingle(parameters[0], CultureInfo.InvariantCulture));
}
