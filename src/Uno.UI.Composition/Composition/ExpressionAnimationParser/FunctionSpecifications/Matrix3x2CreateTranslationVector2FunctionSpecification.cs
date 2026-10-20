#nullable enable

using System.Numerics;

namespace Microsoft.UI.Composition;

internal sealed class Matrix3x2CreateTranslationVector2FunctionSpecification : IAnimationFunctionSpecification
{
	private Matrix3x2CreateTranslationVector2FunctionSpecification()
	{
	}

	public static Matrix3x2CreateTranslationVector2FunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "CreateTranslation";

	public string? ClassName => "Matrix3x2";

	public object Evaluate(params object[] parameters)
		=> Matrix3x2.CreateTranslation((Vector2)parameters[0]);
}
