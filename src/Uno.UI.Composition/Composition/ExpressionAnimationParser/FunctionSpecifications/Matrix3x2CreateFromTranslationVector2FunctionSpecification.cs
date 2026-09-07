#nullable enable

using System.Numerics;

namespace Microsoft.UI.Composition;

internal sealed class Matrix3x2CreateFromTranslationVector2FunctionSpecification : IAnimationFunctionSpecification
{
	private Matrix3x2CreateFromTranslationVector2FunctionSpecification()
	{
	}

	public static Matrix3x2CreateFromTranslationVector2FunctionSpecification Instance { get; } = new();

	public int ParametersLength => 1;

	public string MethodName => "CreateFromTranslation";

	public string? ClassName => "Matrix3x2";

	public object Evaluate(params object[] parameters)
		=> Matrix3x2.CreateTranslation((Vector2)parameters[0]);
}
