#nullable enable

using System.Numerics;

namespace Microsoft.UI.Composition;

internal sealed class Matrix3x2CreateScaleVector2FunctionSpecification : IAnimationFunctionSpecification
{
	private Matrix3x2CreateScaleVector2FunctionSpecification()
	{
	}

	public static Matrix3x2CreateScaleVector2FunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "CreateScale";

	public string? ClassName => "Matrix3x2";

	public object Evaluate(params object[] parameters)
		=> Matrix3x2.CreateScale((Vector2)parameters[0]);
}
